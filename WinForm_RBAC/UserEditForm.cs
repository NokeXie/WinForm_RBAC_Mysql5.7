using DevExpress.XtraEditors;
using System;
using System.Configuration;
using System.Data;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public partial class UserEditForm : XtraForm
    {
        #region --- 字段与属性 ---

        private readonly int _userId;
        private readonly string _connectionString;

        /// <summary>
        /// 获取修改后的用户名
        /// </summary>
        public string NewUserName => txtUserName.Text.Trim();

        /// <summary>
        /// 获取新密码（若为空则表示不修改密码）
        /// </summary>
        public string NewPassword => txtPassword.Text;

        /// <summary>
        /// 获取选中的角色ID
        /// </summary>
        public int NewRoleId => Convert.ToInt32(lkeRole.EditValue);

        #endregion

        #region --- 构造函数 ---

        /// <summary>
        /// 用户编辑窗体
        /// </summary>
        /// <param name="dtRoles">角色列表数据源</param>
        /// <param name="userId">用户ID</param>
        /// <param name="currentUserName">当前用户名</param>
        /// <param name="currentRoleId">当前角色ID</param>
        public UserEditForm(DataTable dtRoles, int userId, string currentUserName, int currentRoleId)
        {
            InitializeComponent();

            // 初始化基础数据
            this._userId = userId;
            this._connectionString = ConfigurationManager.ConnectionStrings["DataBase_Noke_system"].ConnectionString;

            // 1. 配置 LookUpEdit 数据源
            lkeRole.Properties.DataSource = dtRoles;
            lkeRole.Properties.DisplayMember = "RoleName";
            lkeRole.Properties.ValueMember = "RoleID";

            // 2. 列表列配置：清除自动生成的列，仅保留“角色名称”
            lkeRole.Properties.Columns.Clear();
            lkeRole.Properties.Columns.Add(new DevExpress.XtraEditors.Controls.LookUpColumnInfo("RoleName", "角色名称"));

            // 3. UI 细节优化：隐藏下拉列表表头
            lkeRole.Properties.ShowHeader = false;

            // 4. 界面赋初值
            txtUserName.Text = currentUserName;
            lkeRole.EditValue = currentRoleId;
        }

        #endregion

        #region --- 事件处理 ---

        /// <summary>
        /// 点击保存按钮
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            // 1. 校验用户名
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                XtraMessageBox.Show("请输入用户名！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUserName.Focus();
                return;
            }

            // 2. 校验角色（权限）- 核心检测
            if (lkeRole.EditValue == null || lkeRole.EditValue == DBNull.Value || Convert.ToInt32(lkeRole.EditValue) == -1)
            {
                XtraMessageBox.Show("请为用户选择一个角色！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lkeRole.ShowPopup(); // 自动展开下拉列表，提醒用户选择
                return;
            }

            // 3. 校验密码（仅限新增模式）
            if (this._userId == -1 && string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                XtraMessageBox.Show("新增用户必须设置初始密码！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            // 所有校验通过
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        /// <summary>
        /// 点击取消按钮
        /// </summary>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        #endregion
    }
}