using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    /// <summary>
    /// 登录窗体：负责数据库连接初始化、用户身份验证及登录安全控制
    /// </summary>
    public partial class Login_Form : DevExpress.XtraEditors.XtraForm
    {
        #region --- 1. 字段与属性 ---

        /// <summary> 直连 MySQL 的连接字符串，由配置文件解密后构建 </summary>
        public readonly string ConnectionString;

        /// <summary> 当前登录成功后的用户名（静态属性供全局访问） </summary>
        public static string LogonUser { get; private set; }

        #endregion

        #region --- 2. 构造函数与初始化 ---

        /// <summary>
        /// 登录窗体构造函数：完成配置文件读取、密码解密及连接字符串预热
        /// </summary>
        public Login_Form()
        {
            InitializeComponent();

            try
            {
                // 1. 读取原始连接字符串
                string rawConn = ConfigurationManager.ConnectionStrings["WinForm_RBAC"]?.ConnectionString;
                if (string.IsNullOrEmpty(rawConn))
                    throw new Exception("未在配置文件中找到名为 'WinForm_RBAC' 的连接字符串。");

                var builder = new MySqlConnectionStringBuilder(rawConn);

                // 2. 解密连接字符串中的数据库密码 (使用 AES 算法)
                if (!string.IsNullOrEmpty(builder.Password))
                    builder.Password = AESHelper.Decrypt(builder.Password);

                // 3. 设置连接超时时间（防止网络异常时界面长时间假死）
                builder.ConnectionTimeout = 5;

                ConnectionString = builder.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"数据库配置加载失败：{ex.Message}", "初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (Login_simpleButton != null) Login_simpleButton.Enabled = false;
            }
        }

        #endregion

        #region --- 3. UI 交互事件 (Events) ---

        /// <summary>
        /// 登录按钮点击事件：执行异步身份验证、权限加载、单点登录校验及频率控制
        /// </summary>
        private async void Login_simpleButton_Click(object sender, EventArgs e)
        {
            string user = Input_User.Text.Trim();
            string password = Input_password.Text;

            // 1. 基础非空校验
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("请输入用户名和密码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 锁定 UI 状态
            ToggleUIState(true);

            try
            {
                // 2. 环境准备：同步连接字符串到全局配置
                GlobalInfo.ConnectionString = this.ConnectionString;

                // 3. 执行身份验证并加载该用户拥有的所有权限码
                bool isAuthSuccess = await PermissionService.AuthenticateAndLoadPermissionsAsync(user, password);

                if (isAuthSuccess)
                {
                    int userId = GlobalInfo.CurrentUserId;
                    
                    // 4. 安全检查：防止暴力破解（登录频率限制）
                    if (PermissionService.IsLoginTooFrequent(userId, out int waitSec))
                    {
                        DevExpress.XtraEditors.XtraMessageBox.Show(
                            $"登录过于频繁，请在 {waitSec} 秒后再试。",
                            "提示",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return; // 拦截并中断登录流程
                    }

                    // 5. 更新安全数据
                    PermissionService.UpdateLastLoginTime(userId);
                    
                    // 6. 处理单点登录 (SSO) 逻辑：生成并保存新的 Session Token
                    string myToken = Guid.NewGuid().ToString();
                    GlobalInfo.CurrentSessionToken = myToken;
                    
                    // 写入数据库。此操作会导致该用户在其他设备上的旧 Token 失效
                    PermissionService.UpdateUserToken(userId, myToken);

                    // 7. 登录成功，关闭窗体
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
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
                // 解锁 UI 状态
                ToggleUIState(false);
            }
        }

        /// <summary>
        /// 退出按钮点击事件
        /// </summary>
        private void Exit_simpleButton_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        #endregion

        #region --- 4. 内部辅助方法 (Helpers) ---

        /// <summary>
        /// 切换 UI 状态：在登录尝试期间禁用按钮并更改鼠标光标样式
        /// </summary>
        /// <param name="isLoggingIn">是否处于正在登录状态</param>
        private void ToggleUIState(bool isLoggingIn)
        {
            Login_simpleButton.Enabled = !isLoggingIn;
            Login_simpleButton.Text = isLoggingIn ? "正在连接..." : "登录";
            this.Cursor = isLoggingIn ? Cursors.WaitCursor : Cursors.Default;
        }

        #endregion
    }
}