using System;

namespace WinForm_RBAC
{
    /// <summary>
    /// 全局静态类，用于跨窗体访问当前登录用户信息
    /// </summary>
    public static class GlobalInfo
    {
        // 存储当前登录用户的 ID
        public static int CurrentUserId { get; set; }

        // 存储当前登录用户的显示名称
        public static string CurrentUserName { get; set; }
    }
}