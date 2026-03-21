using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq; // 引入 Linq 以便更简洁地操作集合
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    public class PermissionService
    {
        

        // ================= 数据访问层 (DAL) =================

        public static DataTable GetAllRoles()
        {
            var dt = new DataTable();
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                const string sql = "SELECT RoleID, RoleName FROM Roles";
                using (var da = new MySqlDataAdapter(sql, conn))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        public static DataTable GetAllPermissions()
        {
            var dt = new DataTable();
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                const string sql = "SELECT PermissionCode, ParentCode, Description FROM Permissions";
                using (var da = new MySqlDataAdapter(sql, conn))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }

        public static List<string> GetRolePermissions(int roleId)
        {
            var codes = new List<string>();
            const string sql = "SELECT PermissionCode FROM RolePermissions WHERE RoleID = @RoleID";
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@RoleID", MySqlDbType.VarChar).Value = roleId;
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

        public static void SaveRolePermissions(int roleId, List<string> permissionCodes)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        const string delSql = "DELETE FROM RolePermissions WHERE RoleID = @RoleID";
                        using (var delCmd = new MySqlCommand(delSql, conn, transaction))
                        {
                            delCmd.Parameters.Add("@RoleID", MySql.Data.MySqlClient.MySqlDbType.Int32).Value = roleId;
                            delCmd.ExecuteNonQuery();
                        }

                        const string insSql = "INSERT INTO RolePermissions(RoleID, PermissionCode) VALUES(@RoleID, @PermissionCode)";
                        using (var insCmd = new MySqlCommand(insSql, conn, transaction))
                        {
                            insCmd.Parameters.Add("@RoleID", MySql.Data.MySqlClient.MySqlDbType.Int32);
                            insCmd.Parameters.Add("@PermissionCode", MySqlDbType.VarChar, 50);

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

        /// <summary>
        /// 获取包含角色名称的用户详细列表
        /// </summary>
        public static DataTable GetUserDetailList()
        {
            var dt = new DataTable();
            const string sql = @"
        SELECT Users.UserName     AS '用户名',
               Roles.RoleName     AS '角色名',
               Users.Enable       AS '开启状态',
               Users.UserID, 
               UserRoles.UserID   AS UserRoles_UserID,
               UserRoles.RoleID,
               Roles.RoleID       AS Roles_RoleID,
               Users.PasswordHash AS '密码'
        FROM Users
        INNER JOIN UserRoles ON UserRoles.UserID = Users.UserID
        INNER JOIN Roles     ON Roles.RoleID     = UserRoles.RoleID";

            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))

            using (var da = new MySqlDataAdapter(sql, conn))
            {
                da.Fill(dt);
            }
            return dt;
        }

        /// <summary>
        /// 获取属于指定角色的所有用户名
        /// </summary>
        public static List<string> GetUserNamesByRole(int roleId)
        {
            var userNames = new List<string>();
            const string sql = @"
        SELECT u.UserName 
        FROM Users u
        JOIN UserRoles ur ON u.UserID = ur.UserID
        WHERE ur.RoleID = @RID";

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@RID", roleId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                userNames.Add(reader["UserName"].ToString());
                            }
                        }
                    }
                }
            }
            catch { /* 记录日志 */ }
            return userNames;
        }

        // ================= 业务逻辑层 (BLL) =================

        // 1. 处理节点状态联动 (保持不变)
        public static void HandleNodeCheckState(TreeListNode node, bool isChecked)
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
        public static List<string> CollectAllCheckedPermissionCodes(TreeList treeList)
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
        public static void ApplyRolePermissionsToTree(TreeList treeList, List<string> rolePermissionCodes)
        {
            treeList.UncheckAll();
            foreach (TreeListNode node in treeList.Nodes)
            {
                CheckNodeRecursive(node, rolePermissionCodes);
            }
        }

        //编辑用户密码和角色方法
        public static bool UpdateUser(int userId, string userName, string password, int roleId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
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

                        using (var cmd = new MySqlCommand(userSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Name", userName);
                            cmd.Parameters.AddWithValue("@UID", userId);
                            if (!string.IsNullOrEmpty(pwdHash)) cmd.Parameters.AddWithValue("@Pwd", pwdHash);
                            cmd.ExecuteNonQuery();
                        }

                        // 3. 更新角色关联 (UserRoles 表)
                        string roleSql = "UPDATE UserRoles SET RoleID = @RID WHERE UserID = @UID";
                        using (var cmd = new MySqlCommand(roleSql, conn, trans))
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
        /// 新增用户及其角色关联
        /// </summary>
        public static bool AddUser(string userName, string password, int roleId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
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
                        SELECT LAST_INSERT_ID();"; // 获取刚插入的自增 ID

                        int newUserId;
                        using (var cmd = new MySqlCommand(userSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Name", userName);
                            cmd.Parameters.AddWithValue("@Pwd", PasswordHasher.HashPassword(password));
                            newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        // 2. 插入 UserRoles 关联表
                        const string roleSql = "INSERT INTO UserRoles (UserID, RoleID) VALUES (@UID, @RID)";
                        using (var cmd = new MySqlCommand(roleSql, conn, trans))
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

        /// <summary>
        /// 禁用指定用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <returns>是否成功</returns>
        public static bool DisableUser(int userId)
        {
            // 这里使用简单的 SQL 更新
            const string sql = "UPDATE Users SET Enable = 0 WHERE UserID = @UID";

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@UID", userId);
                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch
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
        public static bool EnableUser(int userId)
        {
            // 将 Enable 设为 1，对应 Login_Form 中的验证条件
            const string sql = "UPDATE Users SET Enable = 1 WHERE UserID = @UID";

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
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
        public static bool DeleteUser(int userId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. 先删除角色关联表中的数据 (外键表)
                        const string sqlRoles = "DELETE FROM UserRoles WHERE UserID = @UID";
                        using (var cmd = new MySqlCommand(sqlRoles, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@UID", userId);
                            cmd.ExecuteNonQuery();
                        }

                        // 2. 再删除用户表中的数据 (主表)
                        const string sqlUser = "DELETE FROM Users WHERE UserID = @UID";
                        using (var cmd = new MySqlCommand(sqlUser, conn, trans))
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

        public static bool UpdateUserEnableStatus(int userId, bool isEnabled)
        {
            const string sql = "UPDATE Users SET Enable = @enable WHERE UserID = @id";
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@enable", isEnabled);
                cmd.Parameters.AddWithValue("@id", userId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// 直接修改指定用户的密码
        /// </summary>
        public static bool UpdateUserPasswordDirectly(int userId, string newPwd)
        {
            // 将明文密码加密为哈希
            string passwordHash = PasswordHasher.HashPassword(newPwd);

            const string sql = "UPDATE Users SET PasswordHash = @Pwd WHERE UserID = @UID";

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Pwd", passwordHash);
                        cmd.Parameters.AddWithValue("@UID", userId);

                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 新增角色
        /// </summary>
        /// <param name="roleName">角色名称</param>
        /// <returns>是否成功</returns>
        public static bool AddRole(string roleName)
        {
            // 1. 先检查重名，防止数据库报错
            const string checkSql = "SELECT COUNT(1) FROM Roles WHERE RoleName = @Name";
            const string insertSql = "INSERT INTO Roles (RoleName) VALUES (@Name)";

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();

                    // 检查重复
                    using (var cmdCheck = new MySqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@Name", roleName);
                        int count = Convert.ToInt32(cmdCheck.ExecuteScalar());
                        if (count > 0) return false; // 名称已存在
                    }

                    // 执行插入
                    using (var cmdInsert = new MySqlCommand(insertSql, conn))
                    {
                        cmdInsert.Parameters.AddWithValue("@Name", roleName);
                        return cmdInsert.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 修改角色名称
        /// </summary>
        /// <param name="roleId">角色ID</param>
        /// <param name="newRoleName">新名称</param>
        /// <returns>是否成功</returns>
        public static bool UpdateRoleName(int roleId, string newRoleName)
        {
            // 1. 检查新名称是否已被其他角色占用
            const string checkSql = "SELECT COUNT(1) FROM Roles WHERE RoleName = @Name AND RoleID != @RID";
            const string updateSql = "UPDATE Roles SET RoleName = @Name WHERE RoleID = @RID";

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();

                    // 检查重名
                    using (var cmdCheck = new MySqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@Name", newRoleName);
                        cmdCheck.Parameters.AddWithValue("@RID", roleId);
                        if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0) return false;
                    }

                    // 执行更新
                    using (var cmdUpdate = new MySqlCommand(updateSql, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@Name", newRoleName);
                        cmdUpdate.Parameters.AddWithValue("@RID", roleId);
                        return cmdUpdate.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <returns>0:成功, 1:有用户引用, -1:失败</returns>
        public static int DeleteRole(int roleId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();

                // 1. 检查是否有用户关联（防止误删导致用户丢失角色）
                const string checkUserSql = "SELECT COUNT(1) FROM UserRoles WHERE RoleID = @RID";
                using (var cmdCheck = new MySqlCommand(checkUserSql, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@RID", roleId);
                    if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0) return 1;
                }

                // 2. 使用事务删除权限关联和角色本身
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 先删权限关联（子表）
                        const string delPermSql = "DELETE FROM RolePermissions WHERE RoleID = @RID";
                        using (var cmd = new MySqlCommand(delPermSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@RID", roleId);
                            cmd.ExecuteNonQuery();
                        }

                        // 再删角色本身（主表）
                        const string delRoleSql = "DELETE FROM Roles WHERE RoleID = @RID";
                        using (var cmd = new MySqlCommand(delRoleSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@RID", roleId);
                            cmd.ExecuteNonQuery();
                        }

                        trans.Commit();
                        return 0;
                    }
                    catch
                    {
                        trans.Rollback();
                        return -1;
                    }
                }
            }
        }

        // ================= 私有递归辅助方法 =================

        private static void SetChildNodesCheckedState(TreeListNode parentNode, bool check)
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

        private static void SetParentNodeChecked(TreeListNode node)
        {
            if (node.ParentNode != null)
            {
                node.ParentNode.CheckState = CheckState.Checked;
                SetParentNodeChecked(node.ParentNode);
            }
        }

        private static void CheckParentNodeState(TreeListNode parentNode)
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

        private static void CollectCheckedRecursive(TreeListNode node, List<string> list)
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

        private static void CheckNodeRecursive(TreeListNode node, List<string> rolePermissionCodes)
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
        // 需要引入命名空间
        // using MySql.Data.MySqlClient;
        // using System.Threading.Tasks;

        public static async Task<bool> AuthenticateAndLoadPermissionsAsync(string user, string password)
        {
            // 1. 密码 Hash 处理
            string passwordHash = PasswordHasher.HashPassword(password);

            try
            {
                // 直接使用全局连接字符串
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    await conn.OpenAsync();

                    // --- 步骤 1：验证用户 ---
                    const string authSql = "SELECT UserID FROM Users WHERE UserName = @user AND PasswordHash = @hash AND Enable = 1";
                    object result;
                    using (var cmd = new MySqlCommand(authSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", user);
                        cmd.Parameters.AddWithValue("@hash", passwordHash);
                        result = await cmd.ExecuteScalarAsync();
                    }

                    if (result == null) return false; // 验证失败，返回 false 给 UI 层处理

                    int userId = Convert.ToInt32(result);

                    // --- 步骤 2：加载权限 ---
                    const string permSql = @"
                SELECT DISTINCT p.PermissionCode
                FROM UserRoles ur
                INNER JOIN RolePermissions rp ON ur.RoleID = rp.RoleID
                INNER JOIN Permissions p ON rp.PermissionCode = p.PermissionCode
                WHERE ur.UserID = @userId";

                    UserSession.Permissions.Clear();
                    using (var permCmd = new MySqlCommand(permSql, conn))
                    {
                        permCmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = await permCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                UserSession.Permissions.Add(reader["PermissionCode"].ToString());
                            }
                        }
                    }

                    // --- 步骤 3：记录全局状态 ---
                    GlobalInfo.CurrentUserId = userId;
                    GlobalInfo.CurrentUserName = user;

                    return true;
                }
            }
            catch (Exception ex)
            {
                // 这里可以记录日志，然后向上层抛出异常或返回 false
                throw new Exception("数据库连接或查询异常，请检查配置。", ex);
            }
        }
    }
}