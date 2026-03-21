using DevExpress.XtraBars;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace WinForm_RBAC
{
    /// <summary>
    /// 权限管理器：负责 UI 控件权限码扫描、同步至数据库及权限在 UI 上的应用
    /// </summary>
    public class PermissionManager
    {
        #region --- 1. 字段配置与构造函数 ---

        private readonly Form _parentForm;

        // 数据库字段常量
        private const string ColPermissionCode = "PermissionCode";
        private const string TableName = "Permissions";

        /// <summary> 日志或异常消息订阅事件 </summary>
        public event Action<string> OnLog;

        /// <summary>
        /// 初始化权限管理器
        /// </summary>
        /// <param name="parentForm">需要进行权限管理的父窗体</param>
        public PermissionManager(Form parentForm)
        {
            _parentForm = parentForm;
        }

        #endregion

        #region --- 2. 核心公共接口 (Public APIs) ---

        /// <summary> 
        /// 执行同步：扫描 UI 上的所有 Tag，更新到数据库并清理数据库中已废弃的权限 
        /// </summary>
        public void SyncModulesToDatabase()
        {
            var permDict = new Dictionary<string, string>();

            // 1. 收集所有 BarManager 中的权限 (DevExpress 菜单栏/工具栏)
            foreach (var bm in GetAllBarManagers())
            {
                CollectBarItemTags(bm.Items, permDict);
            }

            // 2. 收集所有普通控件中的权限 (按钮等)
            CollectControlTags(_parentForm.Controls, permDict);

            // 3. 推断父级编码并整理最终字典
            var finalDict = new Dictionary<string, Tuple<string, string>>();
            foreach (var kv in permDict)
            {
                finalDict[kv.Key] = Tuple.Create(kv.Value, InferParentCode(kv.Key, permDict));
            }

            // 4. 将 UI 上的 Tag 同步到数据库 (MERGE)
            SavePermissionsToDatabase(finalDict);

            // 5. 清理数据库中已不存在的 Tag
            RemoveOrphanedPermissions(finalDict.Keys.ToList());
        }

        /// <summary> 
        /// 应用权限：根据 UserSession 中的权限列表，动态显示/隐藏或启用/禁用 UI 控件 
        /// </summary>
        public void ApplyPermissions()
        {
            // 处理 DevExpress 菜单项
            foreach (var bm in GetAllBarManagers())
            {
                ApplyBarItemPermissions(bm.Items);
            }

            // 处理标准 WinForm 控件
            ApplyControlPermissions(_parentForm.Controls);
        }

        #endregion

        #region --- 3. 递归收集逻辑 (Scanning) ---

        /// <summary> 递归收集 BarItem (菜单/按钮) 的 Tag 属性作为权限码 </summary>
        private static void CollectBarItemTags(IEnumerable<BarItem> items, Dictionary<string, string> permDict)
        {
            foreach (var item in items)
            {
                if (item == null) continue;

                string code = item.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(code) && !permDict.ContainsKey(code))
                {
                    // 若无 Caption 则取 Name 作为描述
                    permDict[code] = string.IsNullOrWhiteSpace(item.Caption) ? item.Name : item.Caption;
                }

                // 递归处理子菜单
                if (item is BarSubItem subItem)
                {
                    CollectBarItemTags(subItem.ItemLinks.Select(l => l.Item), permDict);
                }
            }
        }

        /// <summary> 递归收集标准 Control 控件的 Tag 属性作为权限码 </summary>
        private static void CollectControlTags(Control.ControlCollection controls, Dictionary<string, string> permDict)
        {
            foreach (Control ctrl in controls)
            {
                string code = ctrl.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(code) && !permDict.ContainsKey(code))
                {
                    permDict[code] = string.IsNullOrWhiteSpace(ctrl.Text) ? ctrl.Name : ctrl.Text;
                }

                if (ctrl.HasChildren)
                {
                    CollectControlTags(ctrl.Controls, permDict);
                }
            }
        }

        /// <summary> 根据点号分隔符（如 System.User.Add）自动推断父级权限码 </summary>
        private static string InferParentCode(string code, Dictionary<string, string> permDict)
        {
            string current = code;
            while (true)
            {
                int lastDot = current.LastIndexOf('.');
                if (lastDot < 0) return null;

                string candidate = current.Substring(0, lastDot);
                if (permDict.ContainsKey(candidate)) return candidate;

                current = candidate;
            }
        }

        #endregion

        #region --- 4. 递归应用逻辑 (UI Applying) ---

        /// <summary> 执行菜单项的权限应用 (可见性控制) </summary>
        private static void ApplyBarItemPermissions(IEnumerable<BarItem> items)
        {
            foreach (var item in items)
            {
                if (item == null) continue;

                string code = item.Tag?.ToString();
                // 有权限则显示，无权限则彻底隐藏
                item.Visibility = !string.IsNullOrWhiteSpace(code) && UserSession.HasPermission(code)
                    ? BarItemVisibility.Always
                    : BarItemVisibility.Never;

                if (item is BarSubItem subItem)
                {
                    ApplyBarItemPermissions(subItem.ItemLinks.Select(l => l.Item));
                }
            }
        }

        /// <summary> 执行普通控件的权限应用 (可用状态控制) </summary>
        private static void ApplyControlPermissions(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                string code = ctrl.Tag?.ToString();

                if (!string.IsNullOrWhiteSpace(code))
                {
                    // 1. 设置了 Tag 的控件：由权限决定是否可用
                    ctrl.Enabled = UserSession.HasPermission(code);
                }
                else
                {
                    // 2. 未设置 Tag 的按钮类控件：默认禁用（安全性最高原则）
                    if (ctrl is ButtonBase || ctrl.GetType().Name.Contains("Button"))
                    {
                        ctrl.Enabled = false;
                    }
                    else
                    {
                        // 3. 其他非交互控件保持默认开启
                        ctrl.Enabled = true;
                    }
                }

                if (ctrl.HasChildren)
                {
                    ApplyControlPermissions(ctrl.Controls);
                }
            }
        }

        #endregion

        #region --- 5. 数据库持久化逻辑 (Database) ---

        /// <summary> 使用 MySQL 的 MERGE 语法 (ON DUPLICATE KEY) 批量保存权限定义 </summary>
        private void SavePermissionsToDatabase(Dictionary<string, Tuple<string, string>> permDict)
        {
            if (permDict.Count == 0) return;

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    const string sql = @"
                        INSERT INTO Permissions (PermissionCode, Description, ParentCode)
                        VALUES (@Code, @Desc, @Parent)
                        ON DUPLICATE KEY UPDATE 
                            Description = VALUES(Description),
                            ParentCode = VALUES(ParentCode);";

                    foreach (var kv in permDict)
                    {
                        using (var cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@code", kv.Key);
                            cmd.Parameters.AddWithValue("@parent", (object)kv.Value.Item2 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@desc", kv.Value.Item1);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                OnLog?.Invoke($"成功同步 {permDict.Count} 条权限到数据库");
            }
            catch (Exception ex)
            {
                string errMsg = $"同步数据库失败：{ex.Message}";
                OnLog?.Invoke(errMsg);
                MessageBox.Show(errMsg);
            }
        }

        /// <summary> 清理孤立权限：删除数据库中存在但 UI 上已不再使用的权限码 </summary>
        private void RemoveOrphanedPermissions(List<string> currentTags)
        {
            if (currentTags.Count == 0) return;

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();
                    // 动态构建参数化 IN 列表，防止 SQL 注入
                    string paramsList = string.Join(",", currentTags.Select((tag, index) => $"@p{index}"));
                    string sql = $"DELETE FROM {TableName} WHERE {ColPermissionCode} NOT IN ({paramsList})";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        for (int i = 0; i < currentTags.Count; i++)
                        {
                            cmd.Parameters.AddWithValue($"@p{i}", currentTags[i]);
                        }
                        int rowsDeleted = cmd.ExecuteNonQuery();
                        if (rowsDeleted > 0)
                        {
                            OnLog?.Invoke($"清理了 {rowsDeleted} 条不再存在的权限记录");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"清理孤立权限失败：{ex.Message}");
            }
        }

        #endregion

        #region --- 6. 反射辅助工具 ---

        /// <summary> 
        /// 利用反射寻找窗体中所有的 BarManager 实例。
        /// 即使 BarManager 是私有成员且未添加到 Controls 集合中也能找到。
        /// </summary>
        private IEnumerable<BarManager> GetAllBarManagers()
        {
            var result = new List<BarManager>();
            var fields = _parentForm.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (typeof(BarManager).IsAssignableFrom(field.FieldType))
                {
                    var bm = field.GetValue(_parentForm) as BarManager;
                    if (bm != null) result.Add(bm);
                }
            }
            return result;
        }

        #endregion
    }
}