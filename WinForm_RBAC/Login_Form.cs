using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using MySqlProxyClient; // KnockPacket

namespace WinForm_RBAC
{
    public partial class Login_Form : DevExpress.XtraEditors.XtraForm
    {
        // ═══════════════════════════════════════════════════════════
        //  代理 / 敲门配置（与 Program.cs 保持一致）
        // ═══════════════════════════════════════════════════════════
        private const string ProxyHost = "218.3.35.97";
        private const int KnockPort = 9876;
        private const int ProxyPort = 20523;
        private const string KnockKeyHex = "03B821EB8CCDE09A67F1CC3F93B4C908D32BC685B8C8CEB7A42B4F6260F12245";
        private const int MaxRetries = 5;
        private const int BaseDelayMs = 150;
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 已解密、已指向代理端口的 MySQL 连接字符串。
        /// 供 Main_Form / PermissionService 复用同一条连接字符串。
        /// </summary>
        public readonly string connectionString;

        public static string LogonUser { get; private set; }

        public Login_Form()
        {
            // App.config 中只需存放 Database / Uid / Pwd（密文）即可，
            // Server / Port 在运行时强制覆盖为代理地址。
            // 示例连接字符串：
            //   <add name="DataBase_Noke_system"
            //        connectionString="Server=placeholder;Port=0;Database=WinForm_RBAC;Uid=root;Pwd=<密文>" />
            string rawConn = ConfigurationManager
                .ConnectionStrings["WinForm_RBAC"].ConnectionString;

            try
            {
                var builder = new MySqlConnectionStringBuilder(rawConn);

                // 解密密码
                if (!string.IsNullOrEmpty(builder.Password))
                    builder.Password = AESHelper.Decrypt(builder.Password);

                // 强制覆盖为代理地址 + 代理端口
                builder.Server = ProxyHost;
                builder.Port = (uint)ProxyPort;
                builder.ConnectionTimeout = 3; // 配合重试，单次超时短一点

                connectionString = builder.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库配置解密失败：{ex.Message}\n请检查密钥！",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            InitializeComponent();
        }

        // ──────────────────────────────────────────────────────────
        //  登录按钮
        // ──────────────────────────────────────────────────────────
        private void Login_simpleButton_Click(object sender, EventArgs e)
        {
            string user = Input_User.Text.Trim();
            string password = Input_password.Text.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码！", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 禁用按钮，防止重复点击
            Login_simpleButton.Enabled = false;
            Login_simpleButton.Text = "连接中...";

            try
            {
                // ── 第一步：发送 UDP 敲门包 ──────────────────────
                SendKnock();

                // ── 第二步：带退避重试连接代理 ───────────────────
                MySqlConnection conn = ConnectWithRetry();
                if (conn == null)
                {
                    MessageBox.Show(
                        $"已重试 {MaxRetries} 次，仍无法连接到数据库服务。\n请稍后重试或联系管理员。",
                        "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // ── 第三步：验证身份 + 加载权限 ──────────────────
                using (conn)
                {
                    string passwordHash = PasswordHasher.HashPassword(password);

                    // 验证用户身份
                    const string authSql = @"
                        SELECT UserID FROM Users
                        WHERE UserName     = @user
                          AND PasswordHash = @passwordHash
                          AND Enable       = 1";

                    int userId;
                    using (var authCmd = new MySqlCommand(authSql, conn))
                    {
                        authCmd.Parameters.AddWithValue("@user", user);
                        authCmd.Parameters.AddWithValue("@passwordHash", passwordHash);

                        var result = authCmd.ExecuteScalar();
                        if (result == null)
                        {
                            MessageBox.Show("用户名或密码错误，或账号已被禁用！",
                                "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        userId = Convert.ToInt32(result);
                    }

                    // 加载该用户的所有权限
                    const string permSql = @"
                        SELECT p.PermissionCode
                        FROM UserRoles ur
                        INNER JOIN RolePermissions rp ON ur.RoleID         = rp.RoleID
                        INNER JOIN Permissions p      ON rp.PermissionCode = p.PermissionCode
                        WHERE ur.UserID = @userId";

                    UserSession.Permissions.Clear();
                    using (var permCmd = new MySqlCommand(permSql, conn))
                    {
                        permCmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = permCmd.ExecuteReader())
                            while (reader.Read())
                                UserSession.Permissions.Add(reader["PermissionCode"].ToString());
                    }

                    if (UserSession.Permissions.Count == 0)
                        MessageBox.Show("您尚未被分配任何权限，请联系管理员。",
                            "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // 保存全局信息
                    GlobalInfo.CurrentUserId = userId;
                    GlobalInfo.CurrentUserName = user;
                    LogonUser = user;

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"数据库错误 [{ex.Number}]：{ex.Message}",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生未知错误：{ex.Message}",
                    "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Login_simpleButton.Enabled = true;
                Login_simpleButton.Text = "登录";
            }
        }

        private void Exit_simpleButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // ──────────────────────────────────────────────────────────
        //  代理辅助方法（逻辑源自 Program.cs）
        // ──────────────────────────────────────────────────────────

        /// <summary>
        /// 发送 UDP 敲门包，触发服务端把本机 IP 写入白名单。
        /// </summary>
        private static void SendKnock()
        {
            byte[] key = HexToBytes(KnockKeyHex);
            byte[] packet = KnockPacket.Build(key);
            using (var udp = new UdpClient())
                udp.Send(packet, packet.Length, ProxyHost, KnockPort);
        }

        /// <summary>
        /// 发完敲门包后带退避重试连接数据库。
        ///
        /// 为什么需要重试：UDP 无确认，服务端把 IP 写入白名单有时延，
        /// 固定 Sleep 在网络抖动时易失败。重试让连接时机自适应。
        ///
        /// 退避策略：第 i 次失败后等 BaseDelayMs * i 毫秒再重试。
        /// </summary>
        private MySqlConnection ConnectWithRetry()
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                Thread.Sleep(BaseDelayMs * attempt);
                try
                {
                    var conn = new MySqlConnection(connectionString);
                    conn.Open();
                    return conn; // 成功，由调用方 Dispose
                }
                catch (MySqlException ex)
                {
                    // 1130 = Host not allowed（白名单未生效）
                    // 0    = 无法连接到代理
                    bool canRetry = (ex.Number == 1130 || ex.Number == 0)
                                    && attempt < MaxRetries;
                    if (!canRetry) break;
                }
                catch
                {
                    break; // 配置/网络类异常，不值得重试
                }
            }
            return null;
        }

        private static byte[] HexToBytes(string hex)
        {
            hex = hex.Replace(" ", "").Replace("-", "");
            if (hex.Length % 2 != 0)
                throw new FormatException("KnockKeyHex 长度必须为偶数");
            var result = new byte[hex.Length / 2];
            for (int i = 0; i < result.Length; i++)
                result[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return result;
        }
    }
}