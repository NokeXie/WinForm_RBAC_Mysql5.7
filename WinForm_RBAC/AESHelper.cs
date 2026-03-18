using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WinForm_RBAC
{
    public static class AESHelper
    {
        // 建议将 Key 和 IV 换成你自己定义的 16 位字符串
        private static readonly string Key = "Noke_System_2026";
        private static readonly string IV = "8888888888888888";

        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                var encryptor = aes.CreateEncryptor(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(IV));
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
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
    }
}