using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace DXWinForm
{
    public partial class Login_Form : DevExpress.XtraEditors.XtraForm
    {
        private readonly string connectionString;

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

            // 多对多：用户 → UserRoles → Roles → RolePermissions → Permissions

            const string sql = @"
                SELECT p.PermissionCode
                FROM Users u
                INNER JOIN UserRoles ur       ON u.UserID = ur.UserID
                INNER JOIN Roles r            ON ur.RoleID = r.RoleID
                INNER JOIN RolePermissions rp ON r.RoleID = rp.RoleID
                INNER JOIN Permissions p      ON rp.PermissionCode = p.PermissionCode
                WHERE u.UserName     = @user
                  AND u.PasswordHash = @passwordHash
                  AND u.Enable       = 1";

            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@user", user);
                    cmd.Parameters.AddWithValue("@passwordHash", passwordHash);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            UserSession.Permissions.Clear();
                            while (reader.Read())
                            {
                                string pCode = reader["PermissionCode"].ToString();
                                if (!UserSession.Permissions.Contains(pCode))
                                    UserSession.Permissions.Add(pCode);
                            }

                            LogonUser = user;
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("用户名或密码错误，或账号已被禁用！",
                                            "登录失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
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