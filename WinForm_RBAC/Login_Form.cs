using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public partial class Login_Form : DevExpress.XtraEditors.XtraForm
    {
        /// <summary>
        /// 直连 MySQL 的连接字符串，供全局复用1。
        /// </summary>
        public readonly string ConnectionString;

        public static string LogonUser { get; private set; }

        public Login_Form()
        {
            InitializeComponent();

            try
            {
                // 读取并构建 连接字符串
                string rawConn = ConfigurationManager.ConnectionStrings["WinForm_RBAC"]?.ConnectionString;
                if (string.IsNullOrEmpty(rawConn))
                    throw new Exception("未在配置文件中找到名为 'WinForm_RBAC' 的连接字符串。");

                var builder = new MySqlConnectionStringBuilder(rawConn);

                // 解密密码
                if (!string.IsNullOrEmpty(builder.Password))
                    builder.Password = AESHelper.Decrypt(builder.Password);

                builder.ConnectionTimeout = 5;

                ConnectionString = builder.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库配置加载失败：{ex.Message}", "初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (Login_simpleButton != null) Login_simpleButton.Enabled = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  交互逻辑
        // ═══════════════════════════════════════════════════════════

        private async void Login_simpleButton_Click(object sender, EventArgs e)
        {
            string user = Input_User.Text.Trim();
            string password = Input_password.Text;

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ToggleUIState(true);

            try
            {
                // --- 重要修正：先给全局变量赋值 ---
                // 因为 PermissionService 内部会直接使用 GlobalInfo.ConnectionString
                GlobalInfo.ConnectionString = this.ConnectionString;

                // 现在调用 Service，它内部会自动去 GlobalInfo 拿连接
                bool isAuthSuccess = await PermissionService.AuthenticateAndLoadPermissionsAsync(user, password);

                if (isAuthSuccess)
                {
                    int userId = GlobalInfo.CurrentUserId;
                    
                    // --- 新增：登录频率检查 ---
                    if (PermissionService.IsLoginTooFrequent(userId, out int waitSec))
                    {
                        DevExpress.XtraEditors.XtraMessageBox.Show(
                            $"登录过于频繁，请在 {waitSec} 秒后再试。",
                            "提示",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return; // 拦截登录
                    }
                    PermissionService.UpdateLastLoginTime(userId);
                    this.DialogResult = DialogResult.OK;
                    // 生成本次登录的唯一标识（GUID）
                    string myToken = Guid.NewGuid().ToString();

                    // 存入全局变量，方便后面 Timer 拿来对比
                    GlobalInfo.CurrentSessionToken = myToken;

                    // 写入数据库，这步执行完，之前的客户端就会因为 Token 不匹配而被踢出
                    PermissionService.UpdateUserToken(GlobalInfo.CurrentUserId, myToken);
                    this.Close();
                }
                else
                {
                    // 可以在这里加个提示，如果 Service 内部没弹窗的话
                    MessageBox.Show("用户名或密码错误！", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"无法连接到数据库：{ex.Message}", "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"登录过程中发生异常：{ex.Message}", "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ToggleUIState(false);
            }
        }

        private void Exit_simpleButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // ═══════════════════════════════════════════════════════════
        //  内部逻辑方法
        // ═══════════════════════════════════════════════════════════

        private void ToggleUIState(bool isLoggingIn)
        {
            Login_simpleButton.Enabled = !isLoggingIn;
            Login_simpleButton.Text = isLoggingIn ? "正在连接..." : "登录";
            this.Cursor = isLoggingIn ? Cursors.WaitCursor : Cursors.Default;
        }

    }
}