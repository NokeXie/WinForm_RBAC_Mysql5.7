using System;
using System.Security.Cryptography;

namespace MySqlProxyClient
{
    /// <summary>
    /// 敲门包协议（UDP payload，固定 44 字节）
    ///
    /// 布局：
    ///   [0..3]   Magic     4 字节魔数，快速过滤无关流量
    ///   [4..11]  Timestamp UTC Unix 秒 int64 大端
    ///   [12..19] Nonce     随机 8 字节，每次敲门必须不同
    ///   [20..43] HMAC      HMAC-SHA256(key, bytes[0..19]) 取前 24 字节
    ///
    /// 安全保证：
    ///   - 签名验证：无密钥无法伪造
    ///   - 时间窗口：±30s 外的包直接丢弃（防录制重放）
    ///   - Nonce 去重：同一个 Nonce 在窗口内只能使用一次（防即时重放）
    /// </summary>
    public static class KnockPacket
    {
        public const int Size = 44;

        private static readonly byte[] Magic = { 0xDB, 0x4B, 0x4E, 0x4F };
        private const int OffsetTimestamp = 4;
        private const int OffsetNonce = 12;
        private const int OffsetHmac = 20;
        private const int HmacBytes = 24; // HMAC-SHA256 截取前 24 字节

        /// <summary>
        /// 构造一个合法的敲门包（客户端调用）
        /// </summary>
        public static byte[] Build(byte[] sharedKey)
        {
            if (sharedKey == null || sharedKey.Length == 0)
                throw new ArgumentNullException("sharedKey");

            byte[] packet = new byte[Size];

            // Magic
            Buffer.BlockCopy(Magic, 0, packet, 0, 4);

            // Timestamp（大端 int64）
            long ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            WriteInt64BigEndian(packet, OffsetTimestamp, ts);

            // Nonce（8 字节随机）
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(packet, OffsetNonce, 8);

            // HMAC-SHA256(key, packet[0..19])
            // 修复：使用带 offset/count 重载，避免不必要的内存分配
            byte[] hmac = ComputeHmac(sharedKey, packet, 0, OffsetHmac);
            Buffer.BlockCopy(hmac, 0, packet, OffsetHmac, HmacBytes);

            return packet;
        }

        /// <summary>
        /// 解析并验证敲门包（服务端调用）
        /// 返回 true 表示包合法；nonce 为提取出的 8 字节，用于调用方去重
        /// </summary>
        public static bool TryParse(
            byte[] data, int length,
            byte[] sharedKey,
            int toleranceSeconds,
            out byte[] nonce)
        {
            nonce = null;

            // 长度检查
            if (data == null || length != Size)
                return false;

            // 魔数检查（快速路径，减少 HMAC 计算）
            if (data[0] != Magic[0] || data[1] != Magic[1] ||
                data[2] != Magic[2] || data[3] != Magic[3])
                return false;

            // 时间窗口检查（先于 HMAC，减少无效计算）
            long ts = ReadInt64BigEndian(data, OffsetTimestamp);
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long diff = now - ts;
            if (diff < -toleranceSeconds || diff > toleranceSeconds)
                return false;

            // HMAC 验证（恒时比较，防时序攻击）
            byte[] expected = ComputeHmac(sharedKey, data, 0, OffsetHmac);
            if (!ConstantTimeEquals(data, OffsetHmac, expected, 0, HmacBytes))
                return false;

            // 提取 Nonce
            nonce = new byte[8];
            Buffer.BlockCopy(data, OffsetNonce, nonce, 0, 8);
            return true;
        }

        // ── 内部工具 ──────────────────────────────────────────────

        /// <summary>
        /// 修复：使用 ComputeHash(byte[], int, int) 重载，避免中间数组分配
        /// </summary>
        private static byte[] ComputeHmac(byte[] key, byte[] data, int offset, int count)
        {
            using (var hmac = new HMACSHA256(key))
                return hmac.ComputeHash(data, offset, count);
        }

        /// <summary>
        /// 恒时字节比较，防止通过响应时间推断签名内容
        /// </summary>
        private static bool ConstantTimeEquals(
            byte[] a, int aOffset,
            byte[] b, int bOffset,
            int length)
        {
            int diff = 0;
            for (int i = 0; i < length; i++)
                diff |= a[aOffset + i] ^ b[bOffset + i];
            return diff == 0;
        }

        private static void WriteInt64BigEndian(byte[] buf, int offset, long value)
        {
            buf[offset + 0] = (byte)((value >> 56) & 0xFF);
            buf[offset + 1] = (byte)((value >> 48) & 0xFF);
            buf[offset + 2] = (byte)((value >> 40) & 0xFF);
            buf[offset + 3] = (byte)((value >> 32) & 0xFF);
            buf[offset + 4] = (byte)((value >> 24) & 0xFF);
            buf[offset + 5] = (byte)((value >> 16) & 0xFF);
            buf[offset + 6] = (byte)((value >> 8) & 0xFF);
            buf[offset + 7] = (byte)(value & 0xFF);
        }

        private static long ReadInt64BigEndian(byte[] buf, int offset)
        {
            return ((long)buf[offset + 0] << 56)
                 | ((long)buf[offset + 1] << 48)
                 | ((long)buf[offset + 2] << 40)
                 | ((long)buf[offset + 3] << 32)
                 | ((long)buf[offset + 4] << 24)
                 | ((long)buf[offset + 5] << 16)
                 | ((long)buf[offset + 6] << 8)
                 | (long)buf[offset + 7];
        }
    }
}