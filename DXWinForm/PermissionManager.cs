using DevExpress.XtraBars;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DXWinForm
{
    public class PermissionManager
    {
        private readonly string _connectionString;
        private readonly Form _parentForm;

        // 可订阅日志/异常事件
        public event Action<string> OnLog;

        public PermissionManager(Form parentForm, string connectionString)
        {
            _parentForm = parentForm;
            _connectionString = connectionString;
        }

        // =====================================================================
        // 核心流程：同步权限到数据库
        // =====================================================================
        public void SyncModulesToDatabase()
        {
            var permDict = new Dictionary<string, string>();
            foreach (var bm in GetAllBarManagers())
                CollectBarItemTags(bm.Items, permDict);

            CollectControlTags(_parentForm.Controls, permDict);

            var finalDict = new Dictionary<string, Tuple<string, string>>(); // C# 7.3 用 Tuple
            foreach (var kv in permDict)
                finalDict[kv.Key] = Tuple.Create(kv.Value, InferParentCode(kv.Key, permDict));

            SavePermissionsToDatabase(finalDict);
        }

        // =====================================================================
        // 核心流程：应用权限到 UI
        // =====================================================================
        public void ApplyPermissions()
        {
            foreach (var bm in GetAllBarManagers())
                ApplyBarItemPermissions(bm.Items);

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
                    permDict[code] = string.IsNullOrWhiteSpace(item.Caption) ? item.Name : item.Caption;

                if (item is BarSubItem subItem)
                    CollectBarItemTags(subItem.ItemLinks.Select(l => l.Item), permDict);
            }
        }

        private static void CollectControlTags(Control.ControlCollection controls, Dictionary<string, string> permDict)
        {
            foreach (Control ctrl in controls)
            {
                string code = ctrl.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(code) && !permDict.ContainsKey(code))
                    permDict[code] = string.IsNullOrWhiteSpace(ctrl.Text) ? ctrl.Name : ctrl.Text;

                if (ctrl.HasChildren)
                    CollectControlTags(ctrl.Controls, permDict);
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
                    ApplyBarItemPermissions(subItem.ItemLinks.Select(l => l.Item));
            }
        }

        private static void ApplyControlPermissions(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                string code = ctrl.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(code))
                    ctrl.Enabled = UserSession.HasPermission(code);

                if (ctrl.HasChildren)
                    ApplyControlPermissions(ctrl.Controls);
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
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    foreach (var kv in permDict)
                    {
                        const string sql = @"
                                            MERGE Permissions AS target
                                            USING (VALUES (@code, @parent, @desc)) AS source(PermissionCode, ParentCode, Description)
                                            ON target.PermissionCode = source.PermissionCode
                                            WHEN MATCHED THEN
                                                UPDATE SET Description = source.Description, ParentCode = source.ParentCode
                                            WHEN NOT MATCHED THEN
                                                INSERT (PermissionCode, ParentCode, Description)
                                                VALUES (source.PermissionCode, source.ParentCode, source.Description);";

                        using (var cmd = new SqlCommand(sql, conn))
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
                OnLog?.Invoke($"同步数据库失败：{ex.Message}");
                MessageBox.Show($"同步数据库失败：{ex.Message}");
            }
        }

        // =====================================================================
        // 获取窗体中所有 BarManager
        // =====================================================================
        private IEnumerable<BarManager> GetAllBarManagers()
        {
            var result = new List<BarManager>();
            foreach (var field in _parentForm.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (typeof(BarManager).IsAssignableFrom(field.FieldType))
                {
                    var bm = field.GetValue(_parentForm) as BarManager;
                    if (bm != null)
                        result.Add(bm);
                }
            }
            return result;
        }
    }
}