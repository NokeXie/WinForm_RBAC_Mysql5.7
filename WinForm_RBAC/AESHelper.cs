using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WinForm_RBAC
{
    /// <summary>
    /// AES 对称加解密工具类：用于敏感信息（如数据库连接字符串密码）的保护
    /// </summary>
    public static class AESHelper
    {
        #region --- 1. 安全配置 (Keys) ---

        /// <summary> 
        /// 加密密钥 (需为 16, 24 或 32 位) 
        /// </summary>
        private static readonly string Key = "Noke_System_2026";

        /// <summary> 
        /// 初始化向量 (需为 16 位) 
        /// </summary>
        private static readonly string IV = "8888888888888888";

        #endregion

        #region --- 2. 加密逻辑 (Encryption) ---

        /// <summary>
        /// 使用 AES 算法对明文进行加密
        /// </summary>
        /// <param name="plainText">待加密的明文字符串</param>
        /// <returns>加密后的 Base64 编码字符串</returns>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using (Aes aes = Aes.Create())
            {
                // 创建加密器对象
                var encryptor = aes.CreateEncryptor(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(IV));
                
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock(); // 强制刷新最后一块数据
                    }
                    // 将内存流中的二进制数据转换为 Base64 格式，方便在 XML 或文本中存储
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        #endregion

        #region --- 3. 解密逻辑 (Decryption) ---

        /// <summary>
        /// 使用 AES 算法对加密字符串进行解密
        /// </summary>
        /// <param name="cipherText">待解密的 Base64 编码加密字符串</param>
        /// <returns>解密后的明文字符串</returns>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                using (Aes aes = Aes.Create())
                {
                    // 创建解密器对象
                    var decryptor = aes.CreateDecryptor(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(IV));
                    
                    using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (var reader = new StreamReader(cs))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 若解密失败（可能是 Key 不匹配或数据损坏），可根据需要记录日志
                throw new Exception("AES 解密失败，请检查密钥配置或密文完整性。", ex);
            }
        }

        #endregion
    }
}