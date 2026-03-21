using DevExpress.XtraTreeList;
using DevExpress.XtraTreeList.Nodes;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    /// <summary>
    /// 权限与用户管理服务类：处理 RBAC 核心业务逻辑与数据库交互
    /// </summary>
    public class PermissionService
    {
        #region --- 1. 数据查询模块 (Query) ---

        /// <summary> 从数据库获取所有角色信息 </summary>
        /// <returns> 包含 RoleID 和 RoleName 的 DataTable </returns>
        public static DataTable GetAllRoles()
        {
            var dt = new DataTable();
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                const string sql = "SELECT RoleID, RoleName FROM Roles";
                using (var da = new MySqlDataAdapter(sql, conn)) { da.Fill(dt); }
            }
            return dt;
        }

        /// <summary> 从数据库获取所有权限定义（用于构建权限树） </summary>
        /// <returns> 包含代码、父级代码及描述的 DataTable </returns>
        public static DataTable GetAllPermissions()
        {
            var dt = new DataTable();
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                const string sql = "SELECT PermissionCode, ParentCode, Description FROM Permissions";
                using (var da = new MySqlDataAdapter(sql, conn)) { da.Fill(dt); }
            }
            return dt;
        }

        /// <summary> 获取指定角色当前拥有的所有权限代码列表 </summary>
        /// <param name="roleId">角色唯一标识 ID</param>
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
                    while (reader.Read()) { codes.Add(reader["PermissionCode"].ToString()); }
                }
            }
            return codes;
        }

        /// <summary> 获取用户详细列表（联表查询，包含角色名、启用状态、密码哈希等） </summary>
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
            using (var da = new MySqlDataAdapter(sql, conn)) { da.Fill(dt); }
            return dt;
        }

        /// <summary> 根据角色 ID 查询属于该角色的所有用户名（用于删除角色前的检查或显示） </summary>
        public static List<string> GetUserNamesByRole(int roleId)
        {
            var userNames = new List<string>();
            const string sql = "SELECT u.UserName FROM Users u JOIN UserRoles ur ON u.UserID = ur.UserID WHERE ur.RoleID = @RID";
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
                            while (reader.Read()) { userNames.Add(reader["UserName"].ToString()); }
                        }
                    }
                }
            }
            catch { /* 记录日志 */ }
            return userNames;
        }

        #endregion

        #region --- 2. 角色与权限维护 (Management) ---

        /// <summary> 保存角色的权限分配（采用事务处理：先删除旧权限，再插入新勾选的权限） </summary>
        /// <param name="roleId">角色 ID</param>
        /// <param name="permissionCodes">选中的权限代码集合</param>
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
                            delCmd.Parameters.Add("@RoleID", MySqlDbType.Int32).Value = roleId;
                            delCmd.ExecuteNonQuery();
                        }

                        const string insSql = "INSERT INTO RolePermissions(RoleID, PermissionCode) VALUES(@RoleID, @PermissionCode)";
                        using (var insCmd = new MySqlCommand(insSql, conn, transaction))
                        {
                            insCmd.Parameters.Add("@RoleID", MySqlDbType.Int32);
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
                    catch { transaction.Rollback(); throw; }
                }
            }
        }

        /// <summary> 新增角色（包含重名检查） </summary>
        /// <param name="roleName">新角色名称</param>
        /// <returns> 是否新增成功（若名称已存在返回 false） </returns>
        public static bool AddRole(string roleName)
        {
            const string checkSql = "SELECT COUNT(1) FROM Roles WHERE RoleName = @Name";
            const string insertSql = "INSERT INTO Roles (RoleName) VALUES (@Name)";
            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    using (var cmdCheck = new MySqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@Name", roleName);
                        if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0) return false;
                    }
                    using (var cmdInsert = new MySqlCommand(insertSql, conn))
                    {
                        cmdInsert.Parameters.AddWithValue("@Name", roleName);
                        return cmdInsert.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }

        /// <summary> 修改角色名称（包含重名检查，排除自身 ID） </summary>
        public static bool UpdateRoleName(int roleId, string newRoleName)
        {
            const string checkSql = "SELECT COUNT(1) FROM Roles WHERE RoleName = @Name AND RoleID != @RID";
            const string updateSql = "UPDATE Roles SET RoleName = @Name WHERE RoleID = @RID";
            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    using (var cmdCheck = new MySqlCommand(checkSql, conn))
                    {
                        cmdCheck.Parameters.AddWithValue("@Name", newRoleName);
                        cmdCheck.Parameters.AddWithValue("@RID", roleId);
                        if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0) return false;
                    }
                    using (var cmdUpdate = new MySqlCommand(updateSql, conn))
                    {
                        cmdUpdate.Parameters.AddWithValue("@Name", newRoleName);
                        cmdUpdate.Parameters.AddWithValue("@RID", roleId);
                        return cmdUpdate.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch { return false; }
        }

        /// <summary> 删除角色（包含引用检查：若有用户属于该角色则禁止删除） </summary>
        /// <returns> 0:成功, 1:有用户引用, -1:系统失败 </returns>
        public static int DeleteRole(int roleId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                const string checkUserSql = "SELECT COUNT(1) FROM UserRoles WHERE RoleID = @RID";
                using (var cmdCheck = new MySqlCommand(checkUserSql, conn))
                {
                    cmdCheck.Parameters.AddWithValue("@RID", roleId);
                    if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0) return 1;
                }
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        const string delPermSql = "DELETE FROM RolePermissions WHERE RoleID = @RID";
                        using (var cmd = new MySqlCommand(delPermSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@RID", roleId);
                            cmd.ExecuteNonQuery();
                        }
                        const string delRoleSql = "DELETE FROM Roles WHERE RoleID = @RID";
                        using (var cmd = new MySqlCommand(delRoleSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@RID", roleId);
                            cmd.ExecuteNonQuery();
                        }
                        trans.Commit();
                        return 0;
                    }
                    catch { trans.Rollback(); return -1; }
                }
            }
        }

        #endregion

        #region --- 3. 用户管理模块 (User) ---

        /// <summary> 创建新用户并分配角色（事务确保用户与角色关系同步生成） </summary>
        public static bool AddUser(string userName, string password, int roleId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        const string userSql = "INSERT INTO Users (UserName, PasswordHash, Enable) VALUES (@Name, @Pwd, 1); SELECT LAST_INSERT_ID();";
                        int newUserId;
                        using (var cmd = new MySqlCommand(userSql, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Name", userName);
                            cmd.Parameters.AddWithValue("@Pwd", PasswordHasher.HashPassword(password));
                            newUserId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
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
                    catch { trans.Rollback(); return false; }
                }
            }
        }

        /// <summary> 修改用户信息（支持修改用户名、密码、角色；若密码为空则不更新密码） </summary>
        public static bool UpdateUser(int userId, string userName, string password, int roleId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string pwdHash = !string.IsNullOrEmpty(password) ? PasswordHasher.HashPassword(password) : "";
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
                    catch { trans.Rollback(); return false; }
                }
            }
        }

        /// <summary> 彻底删除用户（先删 UserRoles 关联，再删 Users 主表） </summary>
        public static bool DeleteUser(int userId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        const string sqlRoles = "DELETE FROM UserRoles WHERE UserID = @UID";
                        using (var cmd = new MySqlCommand(sqlRoles, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@UID", userId);
                            cmd.ExecuteNonQuery();
                        }
                        const string sqlUser = "DELETE FROM Users WHERE UserID = @UID";
                        using (var cmd = new MySqlCommand(sqlUser, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@UID", userId);
                            cmd.ExecuteNonQuery();
                        }
                        trans.Commit();
                        return true;
                    }
                    catch { trans.Rollback(); return false; }
                }
            }
        }

        /// <summary> 启用用户账号 </summary>
        public static bool EnableUser(int userId) => UpdateUserEnableStatus(userId, true);
        
        /// <summary> 禁用用户账号（禁用后用户将无法登录） </summary>
        public static bool DisableUser(int userId) => UpdateUserEnableStatus(userId, false);

        /// <summary> 更新用户启用/禁用状态的内部通用方法 </summary>
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

        /// <summary> 管理员直接修改指定用户的密码（强行覆盖） </summary>
        public static bool UpdateUserPasswordDirectly(int userId, string newPwd)
        {
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
            catch { return false; }
        }

        #endregion

        #region --- 4. 身份验证与安全控制 (Security) ---

        /// <summary> 异步登录验证：验证通过后加载该用户的所有权限码到全局会话 </summary>
        public static async Task<bool> AuthenticateAndLoadPermissionsAsync(string user, string password)
        {
            string passwordHash = PasswordHasher.HashPassword(password);
            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    await conn.OpenAsync();
                    const string authSql = "SELECT UserID FROM Users WHERE UserName = @user AND PasswordHash = @hash AND Enable = 1";
                    object result;
                    using (var cmd = new MySqlCommand(authSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@user", user);
                        cmd.Parameters.AddWithValue("@hash", passwordHash);
                        result = await cmd.ExecuteScalarAsync();
                    }
                    if (result == null) return false;

                    int userId = Convert.ToInt32(result);
                    const string permSql = @"
                        SELECT DISTINCT p.PermissionCode FROM UserRoles ur
                        INNER JOIN RolePermissions rp ON ur.RoleID = rp.RoleID
                        INNER JOIN Permissions p ON rp.PermissionCode = p.PermissionCode
                        WHERE ur.UserID = @userId";

                    UserSession.Permissions.Clear();
                    using (var permCmd = new MySqlCommand(permSql, conn))
                    {
                        permCmd.Parameters.AddWithValue("@userId", userId);
                        using (var reader = await permCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync()) { UserSession.Permissions.Add(reader["PermissionCode"].ToString()); }
                        }
                    }
                    GlobalInfo.CurrentUserId = userId;
                    GlobalInfo.CurrentUserName = user;
                    return true;
                }
            }
            catch (Exception ex) { throw new Exception("数据库连接或查询异常，请检查配置。", ex); }
        }

        /// <summary> 运行时检查用户是否被踢出（检查 Enable 状态是否变为 0 或账号被删） </summary>
        public static bool CheckIfKicked(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    const string sql = "SELECT Enable FROM Users WHERE UserID = @uid";
                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@uid", userId);
                        object result = cmd.ExecuteScalar();
                        return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) != 1 : true;
                    }
                }
            }
            catch { return false; }
        }

        /// <summary> 登录成功后更新数据库中的 Token（用于单点登录控制） </summary>
        public static void UpdateUserToken(int userId, string token)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                const string sql = "UPDATE Users SET CurrentToken = @token WHERE UserID = @uid";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary> 校验本地 Token 与数据库是否一致（若不一致说明异地有新登录） </summary>
        public static bool IsTokenValid(int userId, string localToken)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT CurrentToken FROM Users WHERE UserID = @uid";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    object dbToken = cmd.ExecuteScalar();
                    return dbToken != null && dbToken.ToString() == localToken;
                }
            }
        }

        /// <summary> 暴力登录防御：检查两次登录时间间隔是否小于 10 秒 </summary>
        public static bool IsLoginTooFrequent(int userId, out int remainingSeconds)
        {
            remainingSeconds = 0;
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT LastLoginTime FROM Users WHERE UserID = @uid";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    object result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        DateTime lastTime = Convert.ToDateTime(result);
                        double diff = (DateTime.Now - lastTime).TotalSeconds;
                        if (diff < 10) { remainingSeconds = 10 - (int)diff; return true; }
                    }
                }
            }
            return false;
        }

        /// <summary> 登录成功后刷新数据库中的最后登录时间 </summary>
        public static void UpdateLastLoginTime(int userId)
        {
            using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
            {
                conn.Open();
                const string sql = "UPDATE Users SET LastLoginTime = NOW() WHERE UserID = @uid";
                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #endregion

        #region --- 5. DevExpress TreeList 交互逻辑 (UI Helpers) ---

        /// <summary> 处理权限树节点的勾选联动逻辑（选中子级必选父级，取消子级联动判断父级） </summary>
        public static void HandleNodeCheckState(TreeListNode node, bool isChecked)
        {
            if (node.Nodes.Count > 0) SetChildNodesCheckedState(node, isChecked);
            if (isChecked) SetParentNodeChecked(node);
            else CheckParentNodeState(node.ParentNode);
        }

        /// <summary> 遍历整棵权限树，收集所有被勾选的权限代码（用于保存到数据库） </summary>
        public static List<string> CollectAllCheckedPermissionCodes(TreeList treeList)
        {
            var selectedCodes = new List<string>();
            foreach (TreeListNode node in treeList.Nodes) CollectCheckedRecursive(node, selectedCodes);
            return selectedCodes;
        }

        /// <summary> 根据传入的权限代码集合，反向勾选 TreeList 中的对应节点 </summary>
        public static void ApplyRolePermissionsToTree(TreeList treeList, List<string> rolePermissionCodes)
        {
            treeList.UncheckAll();
            foreach (TreeListNode node in treeList.Nodes) CheckNodeRecursive(node, rolePermissionCodes);
        }

        /// <summary> 递归设置所有子节点的勾选状态 </summary>
        private static void SetChildNodesCheckedState(TreeListNode parentNode, bool check)
        {
            foreach (TreeListNode child in parentNode.Nodes)
            {
                child.Checked = check;
                if (child.Nodes.Count > 0) SetChildNodesCheckedState(child, check);
            }
        }

        /// <summary> 递归向上勾选所有父节点 </summary>
        private static void SetParentNodeChecked(TreeListNode node)
        {
            if (node.ParentNode != null)
            {
                node.ParentNode.CheckState = CheckState.Checked;
                SetParentNodeChecked(node.ParentNode);
            }
        }

        /// <summary> 递归检查父节点状态：若子节点全部未选中，则取消父节点选中 </summary>
        private static void CheckParentNodeState(TreeListNode parentNode)
        {
            if (parentNode == null) return;
            bool anyChildChecked = parentNode.Nodes.Cast<TreeListNode>().Any(child => child.Checked);
            if (!anyChildChecked)
            {
                parentNode.Checked = false;
                CheckParentNodeState(parentNode.ParentNode);
            }
            else parentNode.CheckState = CheckState.Checked;
        }

        /// <summary> 递归收集勾选代码的辅助方法 </summary>
        private static void CollectCheckedRecursive(TreeListNode node, List<string> list)
        {
            string code = node["PermissionCode"]?.ToString();
            if (!string.IsNullOrEmpty(code) && node.Checked) list.Add(code);
            foreach (TreeListNode child in node.Nodes) CollectCheckedRecursive(child, list);
        }

        /// <summary> 递归执行权限匹配并勾选节点的辅助方法 </summary>
        private static void CheckNodeRecursive(TreeListNode node, List<string> rolePermissionCodes)
        {
            string code = node["PermissionCode"]?.ToString();
            if (!string.IsNullOrEmpty(code) && rolePermissionCodes.Contains(code)) node.Checked = true;
            foreach (TreeListNode childNode in node.Nodes) CheckNodeRecursive(childNode, rolePermissionCodes);
        }

        #endregion
    }
}