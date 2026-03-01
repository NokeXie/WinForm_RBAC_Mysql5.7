using System;
using System.Windows.Forms;

namespace DXWinForm
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. 先实例化登录窗体
            Login_Form loginForm = new Login_Form();

            // 2. 显示登录窗体，并判断其结果是否为 OK
            // 注意：Login_Form 中登录成功时应设置 this.DialogResult = DialogResult.OK;
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                // 3. 如果成功，运行主窗体
                Application.Run(new Main_Form());
            }
            else
            {
                // 4. 如果点击取消或关闭，程序退出
                Application.Exit();
            }
        }
    }
}