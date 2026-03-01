using System;

namespace DXWinForm
{
    // 用于在数据库中存储和同步模块结构
    public class ModuleNode
    {
        // 这里对应数据库的 PermissionCode (这是唯一键)
        public string PermissionCode { get; set; }

        // 这里对应数据库的 Description (用于描述功能)
        public string Description { get; set; }

        // 下面这两个属性用于在扫描 UI 时判断层级，可根据实际需求保留或删除
        public string Name { get; set; }
        public string ParentName { get; set; }
    }
}