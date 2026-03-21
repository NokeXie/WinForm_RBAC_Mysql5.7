namespace WinForm_RBAC
{
    public static class UIHelper
    {
        /// <summary>
        /// 隐藏所有页签
        /// </summary>
        public static void HideAllPages(DevExpress.XtraTab.XtraTabControl xtraTabControl)
        {
            foreach (DevExpress.XtraTab.XtraTabPage tabPage in xtraTabControl.TabPages)
            {
                tabPage.PageVisible = false;
            }
        }

        // 以后你可能还会有其他 UI 需求，比如：
        // public static void SetGridReadOnly(GridView view) { ... }
        // public static void ClearAllInputs(Control container) { ... }
    }
}