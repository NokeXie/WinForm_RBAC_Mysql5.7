using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;
using System;
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

        //编辑用户密码和角色方法
        public bool UpdateUser(int userId, string userName, string password, int roleId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. 处理密码加密
                        string pwdHash = "";
                        if (!string.IsNullOrEmpty(password))
                        {
                            // 调用你的加密类进行转换，确保与登录时的加密方式一致
                            pwdHash = PasswordHasher.HashPassword(password);
                        }

                        // 2. 更新 Users 表
                        string userSql = "UPDATE Users SET UserName = @Name";
                        if (!string.IsNullOrEmpty(pwdHash)) userSql += ", PasswordHash = @Pwd";
                        userSql += " WHERE UserID = @UID";

                        using (var cmd = new SqlCommand(userSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Name", userName);
                            cmd.Parameters.AddWithValue("@UID", userId);
                            if (!string.IsNullOrEmpty(pwdHash)) cmd.Parameters.AddWithValue("@Pwd", pwdHash);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. 更新角色关联 (UserRoles 表)
                        string roleSql = "UPDATE UserRoles SET RoleID = @RID WHERE UserID = @UID";
                        using (var cmd = new SqlCommand(roleSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@RID", roleId);
                            cmd.Parameters.AddWithValue("@UID", userId);
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }
        /// <summary>
        /// 禁用指定用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>是否成功</returns>
        public bool DisableUser(int userId)
        {
            // 这里使用简单的 SQL 更新
            const string sql = "UPDATE Users SET Enable = 0 WHERE UserID = @UID";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UID", userId);
                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // 实际开发中建议在此处记录日志
                return false;
            }
        }
        /// <summary>
        /// 启用用户 (恢复登录权限)
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>是否成功</returns>
        public bool EnableUser(int userId)
        {
            // 将 Enable 设为 1，对应 Login_Form 中的验证条件
            const string sql = "UPDATE Users SET Enable = 1 WHERE UserID = @UID";

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UID", userId);
                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch
            {
                // 此处可记录日志
                return false;
            }
        }
        /// <summary>
        /// 物理删除用户及其所有角色关联
        /// </summary>
        public bool DeleteUser(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. 先删除角色关联表中的数据 (外键表)
                        const string sqlRoles = "DELETE FROM UserRoles WHERE UserID = @UID";
                        using (var cmd = new SqlCommand(sqlRoles, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@UID", userId);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. 再删除用户表中的数据 (主表)
                        const string sqlUser = "DELETE FROM Users WHERE UserID = @UID";
                        using (var cmd = new SqlCommand(sqlUser, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@UID", userId);
                            cmd.ExecuteNonQuery();
                        }

                        // 只有两步都成功，才真正提交到数据库
                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        // 如果任何一步报错，立刻撤销刚才的删除动作
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }
        /// <summary>
        /// 新增用户及其角色关联
        /// </summary>
        public bool AddUser(string userName, string password, int roleId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. 插入 Users 表并获取生成的 UserID
                        const string userSql = @"
                    INSERT INTO Users (UserName, PasswordHash, Enable) 
                    VALUES (@Name, @Pwd, 1);
                    SELECT SCOPE_IDENTITY();"; // 获取刚插入的自增 ID

                        int newUserId;
                        using (var cmd = new SqlCommand(userSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Name", userName);
                            cmd.Parameters.AddWithValue("@Pwd", PasswordHasher.HashPassword(password));
                            newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. 插入 UserRoles 关联表
                        const string roleSql = "INSERT INTO UserRoles (UserID, RoleID) VALUES (@UID, @RID)";
                        using (var cmd = new SqlCommand(roleSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@UID", newUserId);
                            cmd.Parameters.AddWithValue("@RID", roleId);
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return true;
                    }
                    catch
                    {
                        trans.Rollback();
                        return false;
                    }
                }
            }
        }
    }
}