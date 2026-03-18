using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeList;
using System;
using System.Configuration;
using System.Data;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public partial class Main_Form : XtraForm
    {
        #region --- 字段与构造函数 ---

        private readonly PermissionService _permissionService;
        private readonly string _connString;

        public Main_Form()
        {
            InitializeComponent();

            // 初始化连接字符串与服务层
            _connString = ConfigurationManager.ConnectionStrings["DataBase_Noke_system"].ConnectionString;
            _permissionService = new PermissionService(_connString);
        }

        private void Main_Form_Load(object sender, EventArgs e)
        {
            // 初始化权限管理器并同步/应用权限
            var pm = new PermissionManager(this, _connString);

            HideAllPage.HideAllPages(xtraTabControl1);
            pm.ApplyPermissions();
        }

        #endregion

        #region --- 菜单导航事件 ---

        /// <summary>
        /// 用户管理菜单点击
        /// </summary>
        private void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!xtraTabControl1.TabPages.Contains(用户管理))
            {
                xtraTabControl1.TabPages.Add(用户管理);
            }
            用户管理.PageVisible = true;
            xtraTabControl1.SelectedTabPage = 用户管理;
            sqlDataSource1.Fill();
        }

        /// <summary>
        /// 角色管理菜单点击
        /// </summary>
        private void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!xtraTabControl1.TabPages.Contains(角色管理))
            {
                xtraTabControl1.TabPages.Add(角色管理);
            }
            角色管理.PageVisible = true;
            xtraTabControl1.SelectedTabPage = 角色管理;
            InitializeRoleManagement();
        }

        /// <summary>
        /// 标签页关闭事件
        /// </summary>
        private void xtraTabControl1_CloseButtonClick(object sender, EventArgs e)
        {
            if (e is DevExpress.XtraTab.ViewInfo.ClosePageButtonEventArgs arg)
            {
                if (arg.Page is DevExpress.XtraTab.XtraTabPage page)
                {
                    xtraTabControl1.TabPages.Remove(page);
                }
            }
        }

        #endregion

        #region --- 用户管理逻辑 ---

        /// <summary>
        /// 编辑用户信息
        /// </summary>
        private void btnEditUser_Click(object sender, EventArgs e)
        {
            // 1. 记录当前焦点位置
            int savedRowHandle = gridView1.FocusedRowHandle;
            if (savedRowHandle < 0) return;

            // 2. 获取当前行原始数据
            int userId = Convert.ToInt32(gridView1.GetRowCellValue(savedRowHandle, "UserID"));
            string userName = gridView1.GetRowCellValue(savedRowHandle, "用户名")?.ToString();
            int currentRoleId = Convert.ToInt32(gridView1.GetRowCellValue(savedRowHandle, "RoleID"));

            // 3. 弹出编辑对话框
            DataTable dtRoles = _permissionService.GetAllRoles();
            using (var editForm = new UserEditForm(dtRoles, userId, userName, currentRoleId))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    // 执行数据库更新
                    bool isSuccess = _permissionService.UpdateUser(
                        userId,
                        editForm.NewUserName,
                        editForm.NewPassword,
                        editForm.NewRoleId
                    );

                    if (isSuccess)
                    {
                        XtraMessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // 4. 刷新并还原焦点
                        sqlDataSource1.Fill();
                        gridView1.FocusedRowHandle = savedRowHandle;
                        gridView1.SelectRow(savedRowHandle);
                        gridView1.MakeRowVisible(savedRowHandle);
                    }
                }
            }
        }

        #endregion

        #region --- 角色与权限管理逻辑 ---

        /// <summary>
        /// 初始化角色管理界面布局与焦点
        /// </summary>
        private void InitializeRoleManagement()
        {
            object lastRoleId = null;
            if (listBoxControl1.SelectedItem is DevExpress.XtraEditors.Controls.ImageListBoxItem selectedItem)
            {
                lastRoleId = selectedItem.Tag;
            }

            LoadPermissionsTree(treeList1);
            LoadRolesToListBox();

            // 恢复选中项
            if (lastRoleId != null)
            {
                for (int i = 0; i < listBoxControl1.Items.Count; i++)
                {
                    if (listBoxControl1.Items[i] is DevExpress.XtraEditors.Controls.ImageListBoxItem item)
                    {
                        if (item.Tag?.ToString() == lastRoleId.ToString())
                        {
                            listBoxControl1.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }

            if (listBoxControl1.SelectedIndex == -1 && listBoxControl1.Items.Count > 0)
            {
                listBoxControl1.SelectedIndex = 0;
            }
        }

        public void LoadPermissionsTree(TreeList treeList)
        {
            treeList.BeginUpdate();
            try
            {
                treeList.ClearNodes();
                treeList.OptionsView.ShowCheckBoxes = true;
                treeList.OptionsBehavior.AllowRecursiveNodeChecking = false;

                treeList.DataSource = _permissionService.GetAllPermissions();
                treeList.KeyFieldName = "PermissionCode";
                treeList.ParentFieldName = "ParentCode";
                treeList.ExpandAll();

                treeList.AfterCheckNode -= treeList1_AfterCheckNode;
                treeList.AfterCheckNode += treeList1_AfterCheckNode;
            }
            finally
            {
                treeList.EndUpdate();
            }
        }

        private void LoadRolesToListBox()
        {
            listBoxControl1.Items.Clear();
            var dt = _permissionService.GetAllRoles();
            foreach (DataRow row in dt.Rows)
            {
                listBoxControl1.Items.Add(new DevExpress.XtraEditors.Controls.ImageListBoxItem
                {
                    Value = row["RoleName"].ToString(),
                    Tag = row["RoleID"]
                });
            }
        }

        private void treeList1_AfterCheckNode(object sender, NodeEventArgs e)
        {
            if (sender is TreeList tree && e.Node != null)
            {
                tree.BeginUpdate();
                try
                {
                    _permissionService.HandleNodeCheckState(e.Node, e.Node.Checked);
                }
                finally
                {
                    tree.EndUpdate();
                }
            }
        }

        private void listBoxControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxControl1.SelectedItem is DevExpress.XtraEditors.Controls.ImageListBoxItem item)
            {
                int roleId = Convert.ToInt32(item.Tag);
                if (treeList1.Nodes.Count > 0)
                {
                    treeList1.AfterCheckNode -= treeList1_AfterCheckNode;
                    try
                    {
                        var codes = _permissionService.GetRolePermissions(roleId);
                        _permissionService.ApplyRolePermissionsToTree(treeList1, codes);
                    }
                    finally
                    {
                        treeList1.AfterCheckNode += treeList1_AfterCheckNode;
                    }
                }
            }
        }

        /// <summary>
        /// 保存当前角色的权限配置
        /// </summary>
        private void btnSavePermissions_Click(object sender, EventArgs e)
        {
            if (listBoxControl1.SelectedItem is DevExpress.XtraEditors.Controls.ImageListBoxItem item)
            {
                int roleId = Convert.ToInt32(item.Tag);
                var selectedCodes = _permissionService.CollectAllCheckedPermissionCodes(treeList1);
                _permissionService.SaveRolePermissions(roleId, selectedCodes);

                XtraMessageBox.Show("权限保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 刷新权限模块定义
        /// </summary>
        private void simpleButton2_Click(object sender, EventArgs e)
        {
            var pm = new PermissionManager(this, _connString);
            pm.SyncModulesToDatabase();
            InitializeRoleManagement();

            XtraMessageBox.Show("权限模块刷新成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        private void simpleButton4_Click(object sender, EventArgs e)
        {
            // 1. 获取当前选中的行
            int rowHandle = gridView1.FocusedRowHandle;
            if (rowHandle < 0) return;

            // 2. 获取用户信息
            int userId = Convert.ToInt32(gridView1.GetRowCellValue(rowHandle, "UserID"));
            string userName = gridView1.GetRowCellValue(rowHandle, "用户名")?.ToString();

            // 3. 弹出二次确认框，防止误操作
            string message = $"确定要禁用用户「{userName}」吗？\n禁用后该用户将无法登录系统。";
            if (XtraMessageBox.Show(message, "操作确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // 4. 调用服务层执行禁用
                bool isSuccess = _permissionService.DisableUser(userId);

                if (isSuccess)
                {
                    XtraMessageBox.Show("用户已成功禁用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 5. 刷新数据并保留焦点
                    sqlDataSource1.Fill();
                    gridView1.FocusedRowHandle = rowHandle;
                }
                else
                {
                    XtraMessageBox.Show("禁用失败，请检查数据库连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void simpleButton5_Click(object sender, EventArgs e)
        {
            // 1. 获取当前选中的行
            int rowHandle = gridView1.FocusedRowHandle;
            if (rowHandle < 0) return;

            // 2. 获取用户信息
            int userId = Convert.ToInt32(gridView1.GetRowCellValue(rowHandle, "UserID"));
            string userName = gridView1.GetRowCellValue(rowHandle, "用户名")?.ToString();

            // 选做：获取当前状态，避免重复启用 (假设 Grid 中有状态列)
            // object status = gridView1.GetRowCellValue(rowHandle, "状态");

            // 3. 执行启用逻辑
            if (XtraMessageBox.Show($"确定要恢复用户「{userName}」的登录权限吗？", "操作确认",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // 4. 调用服务层
                bool isSuccess = _permissionService.EnableUser(userId);

                if (isSuccess)
                {
                    XtraMessageBox.Show("用户已成功启用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 5. 刷新数据并保留焦点
                    sqlDataSource1.Fill();
                    gridView1.FocusedRowHandle = rowHandle;
                }
                else
                {
                    XtraMessageBox.Show("启用失败，请检查数据库连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnDeleteUser_Click(object sender, EventArgs e)
        {
            // 1. 获取当前选中的行句柄
            int rowHandle = gridView1.FocusedRowHandle;
            if (rowHandle < 0) return;

            // 2. 获取用户信息用于提示
            int userId = Convert.ToInt32(gridView1.GetRowCellValue(rowHandle, "UserID"));
            string userName = gridView1.GetRowCellValue(rowHandle, "用户名")?.ToString();

            // 3. 弹出严厉的确认框
            string confirmMsg = $"危险操作！\n确定要彻底删除用户「{userName}」及其所有权限关联吗？\n删除后数据将无法恢复。";
            if (XtraMessageBox.Show(confirmMsg, "永久删除确认", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) == DialogResult.Yes)
            {
                // 4. 调用服务层执行物理删除
                bool isSuccess = _permissionService.DeleteUser(userId);

                if (isSuccess)
                {
                    XtraMessageBox.Show("用户及关联数据已彻底删除。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 5. 刷新数据
                    sqlDataSource1.Fill();

                    // 6. 自动将焦点移到上一行（因为当前行已消失）
                    if (gridView1.RowCount > 0)
                    {
                        gridView1.FocusedRowHandle = Math.Max(0, rowHandle - 1);
                    }
                }
                else
                {
                    XtraMessageBox.Show("删除失败，该用户可能正在被系统引用或数据库连接异常。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnAddUser_Click(object sender, EventArgs e)
        {
            DataTable dtRoles = _permissionService.GetAllRoles();

            // 传入 -1 代表新增模式
            using (var addForm = new UserEditForm(dtRoles, -1, "", -1))
            {
                // 设置窗口标题为“新增用户”
                addForm.Text = "新增用户";
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    // 此时能进到这里，说明 NewUserName 和 NewPassword 已经过窗体内部校验
                    bool isSuccess = _permissionService.AddUser(
                        addForm.NewUserName,
                        addForm.NewPassword,
                        addForm.NewRoleId
                    );

                    if (isSuccess)
                    {
                        XtraMessageBox.Show("用户新增成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        sqlDataSource1.Fill();
                        gridView1.MoveLast();
                    }
                    else
                    {
                        XtraMessageBox.Show("新增失败，用户名可能已存在。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}