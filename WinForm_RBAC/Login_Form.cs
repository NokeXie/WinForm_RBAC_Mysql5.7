using MySql.Data.MySqlClient;
using MySqlProxyClient; // 需包含 KnockPacket
using System;
using System.Configuration;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public partial class Login_Form : DevExpress.XtraEditors.XtraForm
    {
        // ═══════════════════════════════════════════════════════════
        //  代理 / 敲门配置（优先从 App.config 读取）
        // ═══════════════════════════════════════════════════════════
        private readonly string _proxyHost;
        private readonly int _knockPort;
        private readonly int _proxyPort;
        private readonly string _knockKeyHex;

        private const int MaxRetries = 5;
        private const int BaseDelayMs = 200;

        /// <summary>
        /// 已解密且指向代理端口的连接字符串，供全局复用。
        /// </summary>
        public readonly string ConnectionString;

        public static string LogonUser { get; private set; }

        public Login_Form()
        {
            InitializeComponent();

            try
            {
                // 1. 初始化配置参数
                _proxyHost = ConfigurationManager.AppSettings["ProxyHost"] ?? "218.3.35.97";
                _knockPort = int.TryParse(ConfigurationManager.AppSettings["KnockPort"], out int kp) ? kp : 9876;
                _proxyPort = int.TryParse(ConfigurationManager.AppSettings["ProxyPort"], out int pp) ? pp : 20523;
                _knockKeyHex = ConfigurationManager.AppSettings["KnockKeyHex"] ?? "03B821EB8CCDE09A67F1CC3F93B4C908D32BC685B8C8CEB7A42B4F6260F12245";

                // 2. 构建连接字符串
                string rawConn = ConfigurationManager.ConnectionStrings["WinForm_RBAC"]?.ConnectionString;
                if (string.IsNullOrEmpty(rawConn))
                    throw new Exception("未在配置文件中找到名为 'WinForm_RBAC' 的连接字符串。");

                var builder = new MySqlConnectionStringBuilder(rawConn);

                // 解密密码
                if (!string.IsNullOrEmpty(builder.Password))
                    builder.Password = AESHelper.Decrypt(builder.Password);

                // 强制修正为代理网关地址
                builder.Server = _proxyHost;
                builder.Port = (uint)_proxyPort;
                builder.ConnectionTimeout = 5; // 单次连接超时设定

                ConnectionString = builder.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库配置加载失败：{ex.Message}", "初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // 如果配置失败，禁用登录按钮防止误操作
                if (Login_simpleButton != null) Login_simpleButton.Enabled = false;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  交互逻辑 (Async 异步处理)
        // ═══════════════════════════════════════════════════════════

        private async void Login_simpleButton_Click(object sender, EventArgs e)
        {
            string user = Input_User.Text.Trim();
            string password = Input_password.Text.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 切换 UI 状态
            ToggleUIState(true);

            try
            {
                // 第一步：异步发送敲门包 (Fire and forget 模式)
                await Task.Run(() => SendKnock());

                // 第二步：带退避策略的异步连接重试
                using (MySqlConnection conn = await ConnectWithRetryAsync())
                {
                    if (conn == null)
                    {
                        MessageBox.Show($"重试 {MaxRetries} 次后仍无法连接到安全网关，请检查网络。",
                            "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // 第三步：身份验证与权限加载
                    bool isAuthSuccess = await AuthenticateAndLoadPermissionsAsync(conn, user, password);

                    if (isAuthSuccess)
                    {
                        LogonUser = user;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
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
            Login_simpleButton.Text = isLoggingIn ? "正在安全连接..." : "登录";
            this.Cursor = isLoggingIn ? Cursors.WaitCursor : Cursors.Default;
        }

        /// <summary>
        /// 异步连接重试逻辑（指数退避）
        /// </summary>
        private async Task<MySqlConnection> ConnectWithRetryAsync()
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                // 等待时间随重试次数增加：200ms, 400ms, 600ms...
                await Task.Delay(BaseDelayMs * attempt);

                var conn = new MySqlConnection(ConnectionString);
                try
                {
                    await conn.OpenAsync();
                    return conn; // 成功连接，返回给调用方处理
                }
                catch (MySqlException ex)
                {
                    conn.Dispose();
                    // 1130 = Host not allowed（白名单未生效）, 0 = 无法连接
                    bool canRetry = (ex.Number == 1130 || ex.Number == 0) && attempt < MaxRetries;
                    if (!canRetry) break;
                }
                catch
                {
                    conn.Dispose();
                    break;
                }
            }
            return null;
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

            // 保存全局状态
            GlobalInfo.CurrentUserId = userId;
            GlobalInfo.CurrentUserName = user;
            return true;
        }

        private void SendKnock()
        {
            try
            {
                byte[] key = HexToBytes(_knockKeyHex);
                byte[] packet = KnockPacket.Build(key);
                using (var udp = new UdpClient())
                    udp.Send(packet, packet.Length, _proxyHost, _knockPort);
            }
            catch { /* 敲门静默失败，由连接重试逻辑处理结果 */ }
        }

        private static byte[] HexToBytes(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "");
            if (hex.Length % 2 != 0) throw new FormatException("十六进制密钥长度非法");
            var result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return result;
        }
    }
}