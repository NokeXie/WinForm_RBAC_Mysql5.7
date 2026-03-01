using DevExpress.XtraBars;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Reflection;
using System.Windows.Forms;

namespace DXWinForm
{
    public partial class Main_Form : XtraForm
    {
        private readonly string connectionString;

        public Main_Form()
        {
            InitializeComponent();
            connectionString = ConfigurationManager.ConnectionStrings["DataBase_Noke_system"].ConnectionString;
        }

        private void Main_Form_Load(object sender, EventArgs e)
        {
            SyncModulesToDatabase();
            HideAllPage.HideAllPages(xtraTabControl1);
            ApplyPermissions();
        }

        // =====================================================================
        // 同步权限到数据库
        // 两步走：① 先收集所有 Tag → ② 统一用命名推断 ParentCode
        // =====================================================================
        private void SyncModulesToDatabase()
        {
            // 第一步：只收集 code + desc，不处理层级
            var permDict = new Dictionary<string, string>(); // code → desc

            foreach (var bm in GetAllBarManagers())
                CollectBarItemTags(bm.Items, permDict);

            CollectControlTags(this.Controls, permDict);

            // 第二步：所有 code 都收集完后，统一推断 ParentCode
            // 必须等全部收集完再推断，否则父级可能还没进字典
            var finalDict = new Dictionary<string, (string Desc, string ParentCode)>();
            foreach (var kv in permDict)
                finalDict[kv.Key] = (kv.Value, InferParentCode(kv.Key, permDict));

            SavePermissionsToDatabase(finalDict);
        }

        /// <summary>
        /// 扫描 bm.Items 所有 BarItem，只收集 code + desc，不处理层级。
        /// </summary>
        private static void CollectBarItemTags(BarItems items,
                                               Dictionary<string, string> permDict)
        {
            foreach (BarItem item in items)
            {
                string code = item.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(code) && !permDict.ContainsKey(code))
                    permDict[code] = string.IsNullOrWhiteSpace(item.Caption) ? item.Name : item.Caption;
            }
        }

        /// <summary>
        /// 递归扫描 WinForms 控件，只收集 code + desc，不处理层级。
        /// </summary>
        private static void CollectControlTags(Control.ControlCollection controls,
                                               Dictionary<string, string> permDict)
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

        /// <summary>
        /// 统一的父级推断：逐层去掉最后一段，找到第一个存在于 permDict 的候选。
        /// 必须在所有 code 收集完后调用。
        ///
        ///   SystemManger                → null（"System"不在字典）
        ///   SystemManger.UserManger     → SystemManger          ✓
        ///   SystemManger.UserManger.Add → SystemManger.UserManger ✓
        ///   USER_VIEW.Add               → USER_VIEW             ✓
        /// </summary>
        private static string InferParentCode(string code, Dictionary<string, string> permDict)
        {
            string current = code;
            while (true)
            {
                int lastDot = current.LastIndexOf('.');
                if (lastDot < 0) return null;

                string candidate = current.Substring(0, lastDot);
                if (permDict.ContainsKey(candidate)) return candidate;

                current = candidate; // 继续向上冒泡
            }
        }

        // =====================================================================
        // 应用权限
        // =====================================================================
        private void ApplyPermissions()
        {
            foreach (var bm in GetAllBarManagers())
                ApplyBarItemPermissions(bm.Items);

            ApplyControlPermissions(this.Controls);
        }

        private static void ApplyBarItemPermissions(BarItems items)
        {
            foreach (BarItem item in items)
            {
                string code = item.Tag?.ToString();
                item.Visibility = !string.IsNullOrWhiteSpace(code) && UserSession.HasPermission(code)
                    ? BarItemVisibility.Always
                    : BarItemVisibility.Never;

                if (item is BarSubItem subItem)
                    ApplyBarItemPermissions(subItem.ItemLinks);
            }
        }

        private static void ApplyBarItemPermissions(BarItemLinkCollection links)
        {
            foreach (BarItemLink link in links)
            {
                if (link.Item == null) continue;
                string code = link.Item.Tag?.ToString();
                link.Item.Visibility = !string.IsNullOrWhiteSpace(code) && UserSession.HasPermission(code)
                    ? BarItemVisibility.Always
                    : BarItemVisibility.Never;

                if (link.Item is BarSubItem subItem)
                    ApplyBarItemPermissions(subItem.ItemLinks);
            }
        }

        private static void ApplyControlPermissions(Control.ControlCollection controls)
        {
            foreach (Control ctrl in controls)
            {
                string code = ctrl.Tag?.ToString();

                if (ctrl is SimpleButton)
                    ctrl.Enabled = !string.IsNullOrWhiteSpace(code) && UserSession.HasPermission(code);
                else if (!string.IsNullOrWhiteSpace(code))
                    ctrl.Enabled = UserSession.HasPermission(code);

                if (ctrl.HasChildren)
                    ApplyControlPermissions(ctrl.Controls);
            }
        }

        // =====================================================================
        // 辅助方法
        // =====================================================================
        private void SavePermissionsToDatabase(Dictionary<string, (string Desc, string ParentCode)> permDict)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    foreach (var kv in permDict)
                    {
                        const string sql = @"
                            IF NOT EXISTS (SELECT 1 FROM Permissions WHERE PermissionCode = @code)
                                INSERT INTO Permissions (PermissionCode, ParentCode, Description)
                                VALUES (@code, @parent, @desc)
                            ELSE
                                UPDATE Permissions
                                SET    Description = @desc,
                                       ParentCode  = @parent
                                WHERE  PermissionCode = @code";

                        using (var cmd = new SqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@code", kv.Key);
                            cmd.Parameters.AddWithValue("@parent", (object)kv.Value.ParentCode ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@desc", kv.Value.Desc);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"同步数据库失败：{ex.Message}");
            }
        }

        private IEnumerable<BarManager> GetAllBarManagers()
        {
            var result = new List<BarManager>();
            foreach (var field in this.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (typeof(BarManager).IsAssignableFrom(field.FieldType))
                {
                    var bm = field.GetValue(this) as BarManager;
                    if (bm != null) result.Add(bm);
                }
            }
            return result;
        }

        private void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            用户管理.PageVisible = true;
            xtraTabControl1.SelectedTabPage = 用户管理;
        }
    }
}