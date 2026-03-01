using DevExpress.XtraTab;

namespace DXWinForm
{
    internal class HideAllPage
    {
        public static void HideAllPages(XtraTabControl xtraTabControl)
        {
            foreach (XtraTabPage tabPage in xtraTabControl.TabPages)
            {
                tabPage.PageVisible = false;
            }
        }
    }
}
