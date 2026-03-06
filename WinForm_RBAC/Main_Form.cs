using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using DevExpress.XtraTreeList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public partial class Main_Form : XtraForm
    {
        private readonly PermissionService _permissionService;

        public Main_Form()
        {
            InitializeComponent();
            string connString = ConfigurationManager.ConnectionStrings["DataBase_Noke_system"].ConnectionString;
            // 实例化服务层，负责所有业务逻辑和数据库操作
            _permissionService = new PermissionService(connString);
        }

        private void Main_Form_Load(object sender, EventArgs e)
        {
            // 初始化权限管理器并同步/应用权限
            var pm = new PermissionManager(this, ConfigurationManager.ConnectionStrings["DataBase_Noke_system"].ConnectionString);
            
            HideAllPage.HideAllPages(xtraTabControl1);
            pm.ApplyPermissions();
        }

        #region 菜单点击事件

        private void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            用户管理.PageVisible = true;
            xtraTabControl1.SelectedTabPage = 用户管理;
            sqlDataSource1.Fill();
        }

        private void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            角色管理.PageVisible = true;
            xtraTabControl1.SelectedTabPage = 角色管理;
            InitializeRoleManagement();
        }

        #endregion

        #region 角色管理初始化与加载

        private void InitializeRoleManagement()
        {
            // 1. 加载权限树
            LoadPermissionsTree(treeList1);

            // 2. 加载角色列表
            LoadRolesToListBox();

            // 3. 默认选中第一个角色
            if (listBoxControl1.Items.Count > 0)
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

                // 禁用内置递归，使用自定义联动逻辑
                treeList.OptionsBehavior.AllowRecursiveNodeChecking = false;

                // 从服务获取数据源
                treeList.DataSource = _permissionService.GetAllPermissions();

                // 设置层级字段
                treeList.KeyFieldName = "PermissionCode";
                treeList.ParentFieldName = "ParentCode";

                treeList.ExpandAll();

                // 订阅事件
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
                var item = new DevExpress.XtraEditors.Controls.ImageListBoxItem
                {
                    Value = row["RoleName"].ToString(),
                    Tag = row["RoleID"]
                };
                listBoxControl1.Items.Add(item);
            }
        }

        #endregion

        #region 角色权限处理

        // 自定义事件处理逻辑：调用服务处理节点联动
        private void treeList1_AfterCheckNode(object sender, NodeEventArgs e)
        {
            TreeList tree = sender as TreeList;
            if (tree == null || e.Node == null) return;

            tree.BeginUpdate();
            try
            {
                // 将复杂联动逻辑委托给服务类处理
                _permissionService.HandleNodeCheckState(e.Node, e.Node.Checked);
            }
            finally
            {
                tree.EndUpdate();
            }
        }

        private void listBoxControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxControl1.SelectedItem is DevExpress.XtraEditors.Controls.ImageListBoxItem item)
            {
                int roleId = Convert.ToInt32(item.Tag);
                if (treeList1.Nodes.Count > 0)
                {
                    // 加载前：先摘掉事件，防止每个 node.Checked = true 都触发联动
                    treeList1.AfterCheckNode -= treeList1_AfterCheckNode;
                    try
                    {
                        var codes = _permissionService.GetRolePermissions(roleId);
                        _permissionService.ApplyRolePermissionsToTree(treeList1, codes);
                    }
                    finally
                    {
                        // 加载完：重新挂上事件，恢复用户手动点击的联动功能
                        treeList1.AfterCheckNode += treeList1_AfterCheckNode;
                    }
                }
            }
        }

        #endregion

        #region 保存权限

        private void btnSavePermissions_Click(object sender, EventArgs e)
        {
            if (listBoxControl1.SelectedItem is DevExpress.XtraEditors.Controls.ImageListBoxItem item)
            {
                int roleId = Convert.ToInt32(item.Tag);

                // 调用服务收集所有选中权限
                var selectedCodes = _permissionService.CollectAllCheckedPermissionCodes(treeList1);

                // 调用服务保存数据
                _permissionService.SaveRolePermissions(roleId, selectedCodes);

                XtraMessageBox.Show("权限保存成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            var pm = new PermissionManager(this, ConfigurationManager.ConnectionStrings["DataBase_Noke_system"].ConnectionString);
            pm.SyncModulesToDatabase();
            InitializeRoleManagement();
            XtraMessageBox.Show("权限模块刷新成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}