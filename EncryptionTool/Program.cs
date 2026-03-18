using System;
namespace EncryptionTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("请输入要加密的数据库密码：");
            string pwd = Console.ReadLine();

            // 调用你写好的类
            string secret = WinForm_RBAC.AESHelper.Encrypt(pwd);

            Console.WriteLine("\n加密后的 Base64 字符串为：");
            Console.WriteLine(secret);
            Console.WriteLine("\n请复制上方字符串到 App.config 中。按任意键退出...");
            Console.ReadKey();
        }
    }
}