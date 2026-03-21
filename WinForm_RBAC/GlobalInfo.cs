using System;

namespace WinForm_RBAC
{
    /// <summary>
    /// 全局静态信息类：用于跨窗体共享当前登录用户状态、数据库连接及安全令牌
    /// </summary>
    public static class GlobalInfo
    {
        #region --- 1. 当前用户信息 (User Context) ---

        /// <summary> 
        /// 存储当前登录用户的唯一标识 ID（对应数据库 Users 表主键） 
        /// </summary>
        public static int CurrentUserId { get; set; }

        /// <summary> 
        /// 存储当前登录用户的显示名称（用于界面标题或欢迎词） 
        /// </summary>
        public static string CurrentUserName { get; set; }

        #endregion

        #region --- 2. 数据库配置 (Database Config) ---

        /// <summary> 
        /// 全局数据库连接字符串（由登录窗体初始化并解密后存入） 
        /// </summary>
        public static string ConnectionString { get; set; }

        #endregion

        #region --- 3. 会话安全控制 (Security & Session) ---

        /// <summary> 
        /// 当前登录会话的唯一标识令牌（GUID）。
        /// 用于单点登录 (SSO) 校验，若数据库中的 Token 与此不一致，则强制下线。
        /// </summary>
        public static string CurrentSessionToken { get; set; }

        #endregion
    }
}