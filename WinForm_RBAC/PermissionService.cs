using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq; // 引入 Linq 以便更简洁地操作集合
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public class PermissionService
    {
        private readonly string _connectionString;

        public PermissionService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // ================= 数据访问层 (DAL) =================

        public DataTable GetAllRoles()
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(_connectionString))
            {
                const string sql = "SELECT RoleID, RoleName FROM Roles";
                using (var da = new SqlDataAdapter(sql, conn))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }
        public DataTable GetAllPermissions()
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(_connectionString))
            {
                const string sql = "SELECT PermissionCode, ParentCode, Description FROM Permissions";
                using (var da = new SqlDataAdapter(sql, conn))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        public List<string> GetRolePermissions(int roleId)
        {
            var codes = new List<string>();
            const string sql = "SELECT PermissionCode FROM RolePermissions WHERE RoleID = @RoleID";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@RoleID", SqlDbType.Int).Value = roleId;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        codes.Add(reader["PermissionCode"].ToString());
                    }
                }
            }
            return codes;
        }

        public void SaveRolePermissions(int roleId, List<string> permissionCodes)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string delSql = "DELETE FROM RolePermissions WHERE RoleID = @RoleID";
                        using (var delCmd = new SqlCommand(delSql, conn, transaction))
                        {
                            delCmd.Parameters.Add("@RoleID", SqlDbType.Int).Value = roleId;
                            delCmd.ExecuteNonQuery();
                        }

                        const string insSql = "INSERT INTO RolePermissions(RoleID, PermissionCode) VALUES(@RoleID, @PermissionCode)";
                        using (var insCmd = new SqlCommand(insSql, conn, transaction))
                        {
                            insCmd.Parameters.Add("@RoleID", SqlDbType.Int);
                            insCmd.Parameters.Add("@PermissionCode", SqlDbType.VarChar, 50);

                            foreach (var code in permissionCodes)
                            {
                                insCmd.Parameters["@RoleID"].Value = roleId;
                                insCmd.Parameters["@PermissionCode"].Value = code;
                                insCmd.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ================= 业务逻辑层 (BLL) =================

        // 1. 处理节点状态联动 (保持不变)
        public void HandleNodeCheckState(TreeListNode node, bool isChecked)
        {
            if (node.Nodes.Count > 0)
            {
                SetChildNodesCheckedState(node, isChecked);
            }

            if (isChecked)
            {
                SetParentNodeChecked(node);
            }
            else
            {
                CheckParentNodeState(node.ParentNode);
            }
        }

        // 2. 新增：递归遍历 UI 树，收集所有选中的权限代码
        // 将原 Main_Form 中的 CollectCheckedPermissions 逻辑移动到这里
        public List<string> CollectAllCheckedPermissionCodes(TreeList treeList)
        {
            var selectedCodes = new List<string>();
            foreach (TreeListNode node in treeList.Nodes)
            {
                CollectCheckedRecursive(node, selectedCodes);
            }
            return selectedCodes;
        }

        // 3. 新增：递归遍历 UI 树，根据已有的权限代码勾选节点
        // 将原 Main_Form 中的 CheckNodeRecursive 逻辑移动到这里
        public void ApplyRolePermissionsToTree(TreeList treeList, List<string> rolePermissionCodes)
        {
            treeList.UncheckAll();
            foreach (TreeListNode node in treeList.Nodes)
            {
                CheckNodeRecursive(node, rolePermissionCodes);
            }
        }

        // ================= 私有递归辅助方法 =================

        private void SetChildNodesCheckedState(TreeListNode parentNode, bool check)
        {
            foreach (TreeListNode child in parentNode.Nodes)
            {
                child.Checked = check;
                if (child.Nodes.Count > 0)
                {
                    SetChildNodesCheckedState(child, check);
                }
            }
        }

        private void SetParentNodeChecked(TreeListNode node)
        {
            if (node.ParentNode != null)
            {
                node.ParentNode.CheckState = CheckState.Checked;
                SetParentNodeChecked(node.ParentNode);
            }
        }

        private void CheckParentNodeState(TreeListNode parentNode)
        {
            if (parentNode == null) return;

            // 使用 Linq 检查是否还有子节点选中
            bool anyChildChecked = parentNode.Nodes.Cast<TreeListNode>().Any(child => child.Checked);

            if (!anyChildChecked)
            {
                parentNode.Checked = false;
                CheckParentNodeState(parentNode.ParentNode);
            }
            else
            {
                parentNode.CheckState = CheckState.Checked;
            }
        }

        private void CollectCheckedRecursive(TreeListNode node, List<string> list)
        {
            string code = node["PermissionCode"]?.ToString();
            if (!string.IsNullOrEmpty(code) && node.Checked)
            {
                list.Add(code);
            }

            foreach (TreeListNode child in node.Nodes)
            {
                CollectCheckedRecursive(child, list);
            }
        }

        private void CheckNodeRecursive(TreeListNode node, List<string> rolePermissionCodes)
        {
            string code = node["PermissionCode"]?.ToString();
            if (!string.IsNullOrEmpty(code) && rolePermissionCodes.Contains(code))
            {
                node.Checked = true;
            }

            foreach (TreeListNode childNode in node.Nodes)
            {
                CheckNodeRecursive(childNode, rolePermissionCodes);
            }
        }
    }
}