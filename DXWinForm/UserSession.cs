using System.Collections.Generic;

namespace DXWinForm
{
    public static class UserSession
    {
        // 存放当前用户拥有的所有权限编码
        public static List<string> Permissions { get; set; } = new List<string>();

        // 一个简单的检查方法：判断当前用户是否拥有某项权限
        public static bool HasPermission(string permissionKey)
        {
            if (Permissions == null) return false;
            return Permissions.Contains(permissionKey);
        }
    }
}