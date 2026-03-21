using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public partial class Main_Form : XtraForm
    {
        #region --- 字段与构造函数 ---


        public Main_Form()
        {
            InitializeComponent();
            // 手动初始化 Timer
            checkStatusTimer = new Timer();
            checkStatusTimer.Interval = 10000; // 30秒
            checkStatusTimer.Tick += checkStatusTimer_Tick; // 绑定事件
            checkStatusTimer.Start();

            // 手动绑定事件（如果设计器里没绑定的话）
            this.gridView1.CellValueChanged += gridView1_CellValueChanged;
            this.repositoryItemCheckEdit1.EditValueChanged += repositoryItemCheckEdit1_EditValueChanged;



        }


        private void Main_Form_Load(object sender, EventArgs e)
        {
            // 初始化权限管理器并同步/应用权限
            var pm = new PermissionManager(this);

            UIHelper.HideAllPages(xtraTabControl1);
            pm.ApplyPermissions();
        }

        #endregion

        #region --- 菜单导航事件 ---

        /// <summary>
        /// 用户管理菜单点击
        /// </summary>
        private void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            try
            {
                // 1. UI 逻辑：显示标签页并置顶
                if (!xtraTabControl1.TabPages.Contains(用户管理))
                {
                    xtraTabControl1.TabPages.Add(用户管理);
                }
                用户管理.PageVisible = true;
                xtraTabControl1.SelectedTabPage = 用户管理;

                DataTable userTable = PermissionService.GetUserDetailList();
                gridControl1.DataSource = userTable;


            }
            catch (Exception ex)
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    $"加载用户权限数据失败：{ex.ToString()}", "系统错误");
            }
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
        /// 新增用户
        /// </summary>
        private void btnAddUser_Click(object sender, EventArgs e)
        {
            DataTable dtRoles = PermissionService.GetAllRoles();

            // 传入 -1 代表新增模式
            using (var addForm = new UserEditForm(dtRoles, -1, "", -1))
            {
                // 设置窗口标题为"新增用户"
                addForm.Text = "新增用户";
                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    // 此时能进到这里，说明 NewUserName 和 NewPassword 已经过窗体内部校验
                    bool isSuccess = PermissionService.AddUser(
                        addForm.NewUserName,
                        addForm.NewPassword,
                        addForm.NewRoleId
                    );

                    if (isSuccess)
                    {
                        XtraMessageBox.Show("用户新增成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        DataTable userTable = PermissionService.GetUserDetailList();

                        // 绑定数据到 GridControl
                        gridControl1.DataSource = userTable;
                        gridView1.MoveLast();
                    }
                    else
                    {
                        XtraMessageBox.Show("新增失败，用户名可能已存在。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

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
            DataTable dtRoles = PermissionService.GetAllRoles();
            using (var editForm = new UserEditForm(dtRoles, userId, userName, currentRoleId))
            {
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    // 执行数据库更新
                    bool isSuccess = PermissionService.UpdateUser(
                        userId,
                        editForm.NewUserName,
                        editForm.NewPassword,
                        editForm.NewRoleId
                    );

                    if (isSuccess)
                    {
                        XtraMessageBox.Show("保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // 4. 刷新并还原焦点
                        DataTable userTable = PermissionService.GetUserDetailList();

                        // 绑定数据到 GridControl
                        gridControl1.DataSource = userTable;
                        gridView1.FocusedRowHandle = savedRowHandle;
                        gridView1.SelectRow(savedRowHandle);
                        gridView1.MakeRowVisible(savedRowHandle);
                    }
                }
            }
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        private void simpleButton4_Click(object sender, EventArgs e)
        {
            // 1. 获取当前选中的行
            int rowHandle = gridView1.FocusedRowHandle;
            if (rowHandle < 0) return;

            // 2. 获取用户信息
            int userId = Convert.ToInt32(gridView1.GetRowCellValue(rowHandle, "UserID"));
            string userName = gridView1.GetRowCellValue(rowHandle, "用户名")?.ToString();
        
            if (userId == GlobalInfo.CurrentUserId)
            {
                DevExpress.XtraEditors.XtraMessageBox.Show(
                    "安全警告：您不能禁用或删除当前登录的管理员账号！",
                    "操作受限",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                // 5. 刷新数据并保留焦点
                DataTable userTable = PermissionService.GetUserDetailList();

                // 绑定数据到 GridControl
                gridControl1.DataSource = userTable;
                gridView1.FocusedRowHandle = rowHandle;

                return; // 拦截，不向下执行
            }

            // 3. 弹出二次确认框，防止误操作
            string message = $"确定要禁用用户「{userName}」吗？\n禁用后该用户将无法登录系统。";
            if (XtraMessageBox.Show(message, "操作确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // 4. 调用服务层执行禁用
                bool isSuccess = PermissionService.DisableUser(userId);

                if (isSuccess)
                {
                    XtraMessageBox.Show("用户已成功禁用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 5. 刷新数据并保留焦点
                    DataTable userTable = PermissionService.GetUserDetailList();

                    // 绑定数据到 GridControl
                    gridControl1.DataSource = userTable;
                    gridView1.FocusedRowHandle = rowHandle;
                }
                else
                {
                    XtraMessageBox.Show("禁用失败，请检查数据库连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
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
                bool isSuccess = PermissionService.EnableUser(userId);

                if (isSuccess)
                {
                    XtraMessageBox.Show("用户已成功启用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 5. 刷新数据并保留焦点
                    DataTable userTable = PermissionService.GetUserDetailList();

                    // 绑定数据到 GridControl
                    gridControl1.DataSource = userTable;
                    gridView1.FocusedRowHandle = rowHandle;
                }
                else
                {
                    XtraMessageBox.Show("启用失败，请检查数据库连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 删除用户
        /// </summary>
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
                bool isSuccess = PermissionService.DeleteUser(userId);

                if (isSuccess)
                {
                    XtraMessageBox.Show("用户及关联数据已彻底删除。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 5. 刷新数据
                    DataTable userTable = PermissionService.GetUserDetailList();

                    // 绑定数据到 GridControl
                    gridControl1.DataSource = userTable;

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

        /// <summary>
        /// 修改当前登录用户密码
        /// </summary>
        private void barButtonItem3_ItemClick(object sender, ItemClickEventArgs e)
        {
            // 1. 获取当前登录用户的 ID 和名称
            int currentUserId = GlobalInfo.CurrentUserId;
            string currentUserName = GlobalInfo.CurrentUserName;

            // 2. 直接调用 XtraInputBox.Show 的重载方法
            // 参数说明：提示文字, 标题, 默认值
            string prompt = $"请输入用户「{currentUserName}」的新密码：";
            string caption = "安全设置";

            // 注意：老版本的 XtraInputBox.Show 不支持直接在这里设置 PasswordChar
            // 我们先获取输入结果
            object result = DevExpress.XtraEditors.XtraInputBox.Show(prompt, caption, "");

            // 3. 处理结果
            if (result != null)
            {
                string newPwd = result.ToString().Trim();

                if (string.IsNullOrWhiteSpace(newPwd))
                {
                    DevExpress.XtraEditors.XtraMessageBox.Show("密码不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 4. 调用 Service 层执行更新（内部处理哈希加密）
                bool isSuccess = PermissionService.UpdateUserPasswordDirectly(currentUserId, newPwd);

                if (isSuccess)
                {
                    DevExpress.XtraEditors.XtraMessageBox.Show("密码修改成功！下次登录请使用新密码。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    DevExpress.XtraEditors.XtraMessageBox.Show("修改失败，请检查数据库连接。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Grid 单元格值改变事件（处理"开启状态"列）
        /// </summary>
        private void gridView1_CellValueChanged(object sender, DevExpress.XtraGrid.Views.Base.CellValueChangedEventArgs e)
        {
            // 只处理"开启状态"这一列
            if (e.Column.FieldName == "开启状态")
            {
                var view = sender as DevExpress.XtraGrid.Views.Grid.GridView;
                if (view == null) return;

                // 1. 获取当前行信息

                object userIdObj = view.GetRowCellValue(e.RowHandle, "UserID");
                int targetUserId = Convert.ToInt32(userIdObj);
                object userNameObj = view.GetRowCellValue(e.RowHandle, "用户名");
                if (targetUserId == GlobalInfo.CurrentUserId)
                {
                    DevExpress.XtraEditors.XtraMessageBox.Show(
                        "安全警告：您不能禁用或删除当前登录的管理员账号！",
                        "操作受限",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    // 5. 刷新数据并保留焦点
                    DataTable userTable = PermissionService.GetUserDetailList();

                    // 绑定数据到 GridControl
                    gridControl1.DataSource = userTable;
                    
                    return; // 拦截，不向下执行
                }
                if (userIdObj == null) return;

                int userId = Convert.ToInt32(userIdObj);
                string userName = userNameObj?.ToString() ?? "未知用户";
                bool newStatus = Convert.ToBoolean(e.Value);
                string statusText = newStatus ? "启用" : "禁用";

                // 2. 调用 Service 更新数据库
                try
                {
                    bool success = PermissionService.UpdateUserEnableStatus(userId, newStatus);

                    if (success)
                    {
                        // 成功提示：可以使用简单的弹出框，或者只在界面左下角状态栏提示
                        DevExpress.XtraEditors.XtraMessageBox.Show(
                            $"用户 [{userName}] 已成功{statusText}！",
                            "操作成功",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        throw new Exception("数据库未受影响");
                    }
                }
                catch (Exception ex)
                {
                    // 失败提示
                    DevExpress.XtraEditors.XtraMessageBox.Show(
                        $"更新失败：{ex.Message}",
                        "系统错误",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    // 3. 回滚界面显示：如果数据库更新失败，把界面的勾选状态改回去
                    // 暂时关闭事件响应防止死循环
                    view.CellValueChanged -= gridView1_CellValueChanged;
                    view.SetRowCellValue(e.RowHandle, "开启状态", !newStatus);
                    view.CellValueChanged += gridView1_CellValueChanged;
                }
            }
        }

        /// <summary>
        /// 当复选框状态改变时立即触发
        /// </summary>
        private void repositoryItemCheckEdit1_EditValueChanged(object sender, EventArgs e)
        {
            // 强制 GridView 立即保存当前单元格的编辑并提交到 DataSource
            // 这会直接触发 gridView1_CellValueChanged 事件
            gridView1.PostEditor();
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

        /// <summary>
        /// 加载权限树
        /// </summary>
        public void LoadPermissionsTree(TreeList treeList)
        {
            treeList.BeginUpdate();
            try
            {
                treeList.ClearNodes();
                treeList.OptionsView.ShowCheckBoxes = true;
                treeList.OptionsBehavior.AllowRecursiveNodeChecking = false;

                treeList.DataSource = PermissionService.GetAllPermissions();
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

        /// <summary>
        /// 加载角色列表到 ListBox
        /// </summary>
        private void LoadRolesToListBox()
        {
            listBoxControl1.Items.Clear();
            var dt = PermissionService.GetAllRoles();
            foreach (DataRow row in dt.Rows)
            {
                listBoxControl1.Items.Add(new DevExpress.XtraEditors.Controls.ImageListBoxItem
                {
                    Value = row["RoleName"].ToString(),
                    Tag = row["RoleID"]
                });
            }
        }

        /// <summary>
        /// 权限树节点勾选事件
        /// </summary>
        private void treeList1_AfterCheckNode(object sender, NodeEventArgs e)
        {
            if (sender is TreeList tree && e.Node != null)
            {
                tree.BeginUpdate();
                try
                {
                    PermissionService.HandleNodeCheckState(e.Node, e.Node.Checked);
                }
                finally
                {
                    tree.EndUpdate();
                }
            }
        }

        /// <summary>
        /// 角色列表选中项改变事件
        /// </summary>
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
                        var codes = PermissionService.GetRolePermissions(roleId);
                        PermissionService.ApplyRolePermissionsToTree(treeList1, codes);
                    }
                    finally
                    {
                        treeList1.AfterCheckNode += treeList1_AfterCheckNode;
                    }
                }
            }
        }

        /// <summary>
        /// 新增角色
        /// </summary>
        private void simpleButton1_Click(object sender, EventArgs e)
        {
            // 1. 弹出输入对话框获取新角色名称
            string newRoleName = XtraInputBox.Show("请输入新角色的名称：", "新增角色", "");

            if (!string.IsNullOrWhiteSpace(newRoleName))
            {
                // 2. 调用服务层检查并保存
                bool isSuccess = PermissionService.AddRole(newRoleName.Trim());

                if (isSuccess)
                {
                    XtraMessageBox.Show($"角色「{newRoleName}」新增成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 3. 刷新角色列表
                    LoadRolesToListBox();

                    // 4. 自动选中最后新增的那一项
                    if (listBoxControl1.Items.Count > 0)
                    {
                        listBoxControl1.SelectedIndex = listBoxControl1.Items.Count - 1;
                    }
                }
                else
                {
                    XtraMessageBox.Show("新增失败：角色名称可能已存在或数据库连接异常。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 修改角色名称
        /// </summary>
        private void simpleButton6_Click(object sender, EventArgs e)
        {
            // 1. 验证是否选中了角色
            if (!(listBoxControl1.SelectedItem is DevExpress.XtraEditors.Controls.ImageListBoxItem selectedItem))
            {
                XtraMessageBox.Show("请先选择要修改的角色！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int roleId = Convert.ToInt32(selectedItem.Tag);
            string oldRoleName = selectedItem.Value.ToString();

            // 2. 弹出输入对话框，并赋初始值为旧名称
            string newRoleName = XtraInputBox.Show("请输入新的角色名称：", "修改角色名称", oldRoleName);

            // 3. 只有当名称发生变化且不为空时才执行更新
            if (!string.IsNullOrWhiteSpace(newRoleName) && newRoleName != oldRoleName)
            {
                bool isSuccess = PermissionService.UpdateRoleName(roleId, newRoleName.Trim());

                if (isSuccess)
                {
                    XtraMessageBox.Show("角色名称修改成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 4. 刷新角色列表并保持焦点
                    InitializeRoleManagement();
                }
                else
                {
                    XtraMessageBox.Show("修改失败：名称可能已存在或数据库连接异常。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        private void simpleButton3_Click(object sender, EventArgs e)
        {
            // 1. 获取选中的角色
            if (!(listBoxControl1.SelectedItem is DevExpress.XtraEditors.Controls.ImageListBoxItem selectedItem))
            {
                XtraMessageBox.Show("请先选择要删除的角色！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int roleId = Convert.ToInt32(selectedItem.Tag);
            string roleName = selectedItem.Value.ToString();

            // 2. 核心校验：获取正在使用该角色的用户名单
            List<string> activeUsers = PermissionService.GetUserNamesByRole(roleId);

            if (activeUsers.Count > 0)
            {
                // 拼接前 5 个用户名，防止名单过长导致弹窗撑爆屏幕
                string userListStr = string.Join("、", activeUsers.Take(5));
                if (activeUsers.Count > 5) userListStr += $" 等共 {activeUsers.Count} 人";

                string warningMsg = $"无法删除角色「{roleName}」！\n\n当前以下用户正在使用该角色：\n【{userListStr}】\n\n请先在“用户管理”中更改这些用户的角色后再试。";

                XtraMessageBox.Show(warningMsg, "删除受阻", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // 直接拦截，不进入删除确认
            }

            // 3. 只有无用户关联时，才弹出删除确认
            string confirmMsg = $"确定要永久删除角色「{roleName}」吗？\n此操作将同时清除该角色的所有权限配置。";
            if (XtraMessageBox.Show(confirmMsg, "最终确认", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) == DialogResult.Yes)
            {
                int result = PermissionService.DeleteRole(roleId);

                if (result == 0)
                {
                    XtraMessageBox.Show("角色已成功删除。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // 在删除前记录当前索引
                    int deletedIndex = listBoxControl1.SelectedIndex;

                    LoadRolesToListBox();
                    // 落焦到上一项，兜底第 0 项
                    if (listBoxControl1.Items.Count > 0)
                    {
                        listBoxControl1.SelectedIndex = Math.Max(0, deletedIndex - 1);
                    }
                    else
                    {
                        // 删的是最后一个角色：手动清空权限树，避免显示脏数据
                        treeList1.ClearNodes();
                    }
                }
                else
                {
                    XtraMessageBox.Show("删除失败，请检查数据库连接或权限设置。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                var selectedCodes = PermissionService.CollectAllCheckedPermissionCodes(treeList1);
                PermissionService.SaveRolePermissions(roleId, selectedCodes);

                XtraMessageBox.Show("权限保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 刷新权限模块定义
        /// </summary>
        private void simpleButton2_Click(object sender, EventArgs e)
        {
            var pm = new PermissionManager(this);
            pm.SyncModulesToDatabase();
            InitializeRoleManagement();

            XtraMessageBox.Show("权限模块刷新成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void checkStatusTimer_Tick(object sender, EventArgs e)
        {
            // 使用 Task.Run 异步执行，避免阻塞 UI 线程
            Task.Run(() =>
            {
                try
                {
                    // 1. 先检查是否被管理员禁用
                    bool isKicked = PermissionService.CheckIfKicked(GlobalInfo.CurrentUserId);

                    // 2. 再检查 Token 是否一致 (限制多开)
                    bool isTokenMatch = PermissionService.IsTokenValid(GlobalInfo.CurrentUserId, GlobalInfo.CurrentSessionToken);

                    if (isKicked || !isTokenMatch)
                    {
                        // 回到 UI 线程处理强制退出逻辑
                        this.Invoke(new Action(() =>
                        {
                            checkStatusTimer.Stop();

                            string reason = isKicked ? "您的账号已被管理员禁用。" : "您的账号在另一台设备登录，当前连接已断开。";

                            DevExpress.XtraEditors.XtraMessageBox.Show(reason, "系统提示",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);

                            Application.Exit();
                        }));
                    }
                }
                catch (Exception ex)
                {
                    // 这里处理数据库闪断或网络问题
                    // 建议：仅记录日志或静默处理，不要在这里弹窗，否则每30秒弹一个报错会吵死用户
                    MessageBox.Show(@"状态检查循环发生异常（可能是网络波动）: " + ex.Message);

                    // 如果你想在网络彻底断开时提醒用户，可以增加一个计数器，连续失败 N 次再提示
                }
            });
        }

        #endregion
        // Main_Form.cs 中的计时器事件

    }
}