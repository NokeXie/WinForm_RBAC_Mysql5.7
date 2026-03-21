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
                using (var conn = new MySqlConnection(ConnectionString))
                {
                    await conn.OpenAsync();

                    bool isAuthSuccess = await AuthenticateAndLoadPermissionsAsync(conn, user, password);

                    if (isAuthSuccess)
                    {
                        
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
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

        /// <summary>
        /// 验证身份并加载权限
        /// </summary>
        private async Task<bool> AuthenticateAndLoadPermissionsAsync(MySqlConnection conn, string user, string password)
        {
            string passwordHash = PasswordHasher.HashPassword(password);

            // 1. 验证用户
            const string authSql = "SELECT UserID FROM Users WHERE UserName = @user AND PasswordHash = @hash AND Enable = 1";
            object result;
            using (var cmd = new MySqlCommand(authSql, conn))
            {
                cmd.Parameters.AddWithValue("@user", user);
                cmd.Parameters.AddWithValue("@hash", passwordHash);
                result = await cmd.ExecuteScalarAsync();
            }

            if (result == null)
            {
                MessageBox.Show("用户名或密码错误，或账号已被禁用！", "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            int userId = Convert.ToInt32(result);

            // 2. 加载权限
            const string permSql = @"
                SELECT DISTINCT p.PermissionCode
                FROM UserRoles ur
                INNER JOIN RolePermissions rp ON ur.RoleID = rp.RoleID
                INNER JOIN Permissions p ON rp.PermissionCode = p.PermissionCode
                WHERE ur.UserID = @userId";

            UserSession.Permissions.Clear();
            using (var permCmd = new MySqlCommand(permSql, conn))
            {
                permCmd.Parameters.AddWithValue("@userId", userId);
                using (var reader = await permCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        UserSession.Permissions.Add(reader["PermissionCode"].ToString());
                    }
                }
            }

            if (UserSession.Permissions.Count == 0)
            {
                MessageBox.Show("您尚未被分配任何权限，请联系管理员。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            GlobalInfo.CurrentUserId = userId;
            GlobalInfo.CurrentUserName = user;
            return true;
        }
    }
}