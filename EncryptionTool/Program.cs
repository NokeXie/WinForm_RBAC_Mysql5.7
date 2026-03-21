using System;
using System.Windows.Forms; // 必须引用此命名空间以使用剪贴板

namespace EncryptionTool
{
    /// <summary>
    /// 数据库密码加密辅助工具：
    /// 加密后自动将结果复制到剪贴板，方便直接粘贴。
    /// </summary>
    class Program
    {
        [STAThread] // 操作剪贴板必须添加此特性
        static void Main(string[] args)
        {
            Console.Title = "RBAC 数据库密码加密工具 (自动复制版)";

            Console.WriteLine("==================================================");
            Console.WriteLine("          RBAC 系统数据库密码加密工具");
            Console.WriteLine("==================================================");

            #region --- 1. 获取输入 ---

            Console.WriteLine("\n请输入要加密的【数据库明文密码】：");
            string pwd = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(pwd))
            {
                Console.WriteLine("\n[错误] 密码不能为空！按任意键退出...");
                Console.ReadKey();
                return;
            }

            #endregion

            #region --- 2. 执行加密与自动复制 ---

            try
            {
                // 调用 WinForm_RBAC 中的 AESHelper 加密逻辑
                string secret = WinForm_RBAC.AESHelper.Encrypt(pwd);

                // --- 核心功能：直接写入系统剪贴板 ---
                Clipboard.SetText(secret);

                #region --- 3. 输出提示 ---

                Console.WriteLine("\n--------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("【加密成功并已自动复制到剪贴板！】");
                Console.ResetColor();
                Console.WriteLine("--------------------------------------------------");

                Console.WriteLine("\n加密结果：");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(secret);
                Console.ResetColor();

                Console.WriteLine("\n--------------------------------------------------");
                Console.WriteLine("操作完成：你现在可以直接在 App.config 中按 [Ctrl+V] 粘贴。");
                Console.WriteLine("--------------------------------------------------");

                #endregion
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[异常] 加密失败：{ex.Message}");
                Console.ResetColor();
            }

            #endregion

            Console.WriteLine("\n按任意键关闭窗口...");
            Console.ReadKey();
        }
    }
}