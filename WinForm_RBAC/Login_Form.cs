using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public partial class Login_Form : DevExpress.XtraEditors.XtraForm
    {
        public readonly string connectionString;

        public static string LogonUser { get; private set; }

        public Login_Form()
        {
            connectionString = ConfigurationManager.ConnectionStrings["DataBase_Noke_system"].ConnectionString;
            InitializeComponent();
        }

        private void Login_simpleButton_Click(object sender, EventArgs e)
        {
            string user = Input_User.Text.Trim();
            string password = Input_password.Text.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string passwordHash = PasswordHasher.HashPassword(password);

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // ── 第一步：验证用户身份 ──────────────────────────────
                    const string authSql = @"
                SELECT UserID FROM Users
                WHERE UserName     = @user
                  AND PasswordHash = @passwordHash
                  AND Enable       = 1";

                    int userId;
                    using (var authCmd = new SqlCommand(authSql, conn))
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

                    // ── 第二步：查询权限 ──────────────────────────────────
                    const string permSql = @"
                SELECT p.PermissionCode
                FROM UserRoles ur
                INNER JOIN RolePermissions rp ON ur.RoleID = rp.RoleID
                INNER JOIN Permissions p      ON rp.PermissionCode = p.PermissionCode
                WHERE ur.UserID = @userId";

                    UserSession.Permissions.Clear();
                    using (var permCmd = new SqlCommand(permSql, conn))
                    {
                        permCmd.Parameters.AddWithValue("@userId", userId);

                        using (var reader = permCmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UserSession.Permissions.Add(reader["PermissionCode"].ToString());
                            }
                        }
                    }

                    // 权限为空时给出明确提示，但仍允许登录
                    if (UserSession.Permissions.Count == 0)
                    {
                        MessageBox.Show("您尚未被分配任何权限，请联系管理员。",
                                        "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    LogonUser = user;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"数据库连接失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发生未知错误：{ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Exit_simpleButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}