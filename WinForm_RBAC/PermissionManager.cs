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
    public class PermissionManager
    {
       
        private readonly Form _parentForm;

        // 数据库字段常量，方便修改
        private const string ColPermissionCode = "PermissionCode";
        private const string TableName = "Permissions";

        // 可订阅日志/异常事件
        public event Action<string> OnLog;

        public PermissionManager(Form parentForm)
        {
            _parentForm = parentForm;
            
        }

        // =====================================================================
        // 核心流程：同步权限到数据库 (更新 + 清理)
        // =====================================================================
        public void SyncModulesToDatabase()
        {
            var permDict = new Dictionary<string, string>();

            // 收集所有 BarManager 中的权限
            foreach (var bm in GetAllBarManagers())
            {
                CollectBarItemTags(bm.Items, permDict);
            }

            // 收集所有普通控件中的权限
            CollectControlTags(_parentForm.Controls, permDict);

            // 推断父级编码并整理数据
            var finalDict = new Dictionary<string, Tuple<string, string>>();
            foreach (var kv in permDict)
            {
                finalDict[kv.Key] = Tuple.Create(kv.Value, InferParentCode(kv.Key, permDict));
            }

            // 1. 将 UI 上的 Tag 同步到数据库 (MERGE)
            SavePermissionsToDatabase(finalDict);

            // 2. 清理数据库中已不存在的 Tag
            RemoveOrphanedPermissions(finalDict.Keys.ToList());
        }

        // =====================================================================
        // 核心流程：应用权限到 UI
        // =====================================================================
        public void ApplyPermissions()
        {
            foreach (var bm in GetAllBarManagers())
            {
                ApplyBarItemPermissions(bm.Items);
            }

            ApplyControlPermissions(_parentForm.Controls);
        }

        // =====================================================================
        // 收集方法（统一递归）
        // =====================================================================
        private static void CollectBarItemTags(IEnumerable<BarItem> items, Dictionary<string, string> permDict)
        {
            foreach (var item in items)
            {
                if (item == null) continue;

                string code = item.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(code) && !permDict.ContainsKey(code))
                {
                    permDict[code] = string.IsNullOrWhiteSpace(item.Caption) ? item.Name : item.Caption;
                }

                if (item is BarSubItem subItem)
                {
                    CollectBarItemTags(subItem.ItemLinks.Select(l => l.Item), permDict);
                }
            }
        }

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

        // =====================================================================
        // 应用方法（统一递归）
        // =====================================================================
        private static void ApplyBarItemPermissions(IEnumerable<BarItem> items)
        {
            foreach (var item in items)
            {
                if (item == null) continue;

                string code = item.Tag?.ToString();
                item.Visibility = !string.IsNullOrWhiteSpace(code) && UserSession.HasPermission(code)
                    ? BarItemVisibility.Always
                    : BarItemVisibility.Never;

                if (item is BarSubItem subItem)
                {
                    ApplyBarItemPermissions(subItem.ItemLinks.Select(l => l.Item));
                }
            }
        }

        private static void ApplyControlPermissions(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                string code = ctrl.Tag?.ToString();

                if (!string.IsNullOrWhiteSpace(code))
                {
                    // 有 Tag 的控件按权限判断
                    ctrl.Enabled = UserSession.HasPermission(code);
                }
                else
                {
                    // 没有 Tag 的按钮类控件默认不可用
                    if (ctrl is ButtonBase || ctrl.GetType().Name.Contains("Button"))
                    {
                        ctrl.Enabled = false;
                    }
                    else
                    {
                        // 其他控件保持默认状态
                        ctrl.Enabled = true;
                    }
                }

                // 递归处理子控件
                if (ctrl.HasChildren)
                {
                    ApplyControlPermissions(ctrl.Controls);
                }
            }
        }

        // =====================================================================
        // 数据库操作（使用 MERGE SQL）
        // =====================================================================
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

        // --- 清理孤立的权限记录 ---
        private void RemoveOrphanedPermissions(List<string> currentTags)
        {
            if (currentTags.Count == 0) return;

            try
            {
                using (var conn = new MySqlConnection(GlobalInfo.ConnectionString))
                {
                    conn.Open();

                    // 构建一个 NOT IN 的参数化列表
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

        // =====================================================================
        // 获取窗体中所有 BarManager (利用反射)
        // =====================================================================
        private IEnumerable<BarManager> GetAllBarManagers()
        {
            var result = new List<BarManager>();
            var fields = _parentForm.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                if (typeof(BarManager).IsAssignableFrom(field.FieldType))
                {
                    var bm = field.GetValue(_parentForm) as BarManager;
                    if (bm != null)
                    {
                        result.Add(bm);
                    }
                }
            }
            return result;
        }
    }
}