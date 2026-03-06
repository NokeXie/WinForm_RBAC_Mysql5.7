/*
 Navicat Premium Dump SQL

 Source Server         : noke
 Source Server Type    : SQL Server
 Source Server Version : 10502500 (10.50.2500)
 Source Host           : 127.0.0.1:1433
 Source Catalog        : WinForm_RBAC
 Source Schema         : dbo

 Target Server Type    : SQL Server
 Target Server Version : 10502500 (10.50.2500)
 File Encoding         : 65001

 Date: 06/03/2026 20:43:20
*/


-- ----------------------------
-- Table structure for Permissions
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[Permissions]') AND type IN ('U'))
	DROP TABLE [dbo].[Permissions]
GO

CREATE TABLE [dbo].[Permissions] (
  [PermissionCode] varchar(100) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [ParentCode] varchar(100) COLLATE Chinese_PRC_CI_AS  NULL,
  [Description] varchar(200) COLLATE Chinese_PRC_CI_AS  NULL,
  [SortOrder] int DEFAULT 0 NULL
)
GO

ALTER TABLE [dbo].[Permissions] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of Permissions
-- ----------------------------
INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger', NULL, N'系统管理', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.LogOff', N'SystemManger', N'注销系统', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.PassWord', N'SystemManger', N'修改密码', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.RoleManger', N'SystemManger', N'角色管理', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.RoleManger.Refresh_Permission', N'SystemManger.RoleManger', N'刷新权限模块', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.RoleManger.Save', N'SystemManger.RoleManger', N'保存角色', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.UserManger', N'SystemManger', N'用户管理', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.UserManger.Add', N'SystemManger.UserManger', N'新增用户', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.UserManger.Delete', N'SystemManger.UserManger', N'删除用户', N'0')
GO

INSERT INTO [dbo].[Permissions] ([PermissionCode], [ParentCode], [Description], [SortOrder]) VALUES (N'SystemManger.UserManger.Edit', N'SystemManger.UserManger', N'编辑用户', N'0')
GO


-- ----------------------------
-- Table structure for RolePermissions
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[RolePermissions]') AND type IN ('U'))
	DROP TABLE [dbo].[RolePermissions]
GO

CREATE TABLE [dbo].[RolePermissions] (
  [RoleID] int  NOT NULL,
  [PermissionCode] varchar(100) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[RolePermissions] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of RolePermissions
-- ----------------------------
INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.LogOff')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.PassWord')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.RoleManger')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.RoleManger.Refresh_Permission')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.RoleManger.Save')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.UserManger')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.UserManger.Add')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.UserManger.Delete')
GO

INSERT INTO [dbo].[RolePermissions] ([RoleID], [PermissionCode]) VALUES (N'1', N'SystemManger.UserManger.Edit')
GO


-- ----------------------------
-- Table structure for Roles
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[Roles]') AND type IN ('U'))
	DROP TABLE [dbo].[Roles]
GO

CREATE TABLE [dbo].[Roles] (
  [RoleID] int  IDENTITY(1,1) NOT NULL,
  [RoleName] varchar(50) COLLATE Chinese_PRC_CI_AS  NOT NULL
)
GO

ALTER TABLE [dbo].[Roles] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of Roles
-- ----------------------------
SET IDENTITY_INSERT [dbo].[Roles] ON
GO

INSERT INTO [dbo].[Roles] ([RoleID], [RoleName]) VALUES (N'1', N'系统管理员')
GO

INSERT INTO [dbo].[Roles] ([RoleID], [RoleName]) VALUES (N'2', N'系统管理员1')
GO

SET IDENTITY_INSERT [dbo].[Roles] OFF
GO


-- ----------------------------
-- Table structure for UserRoles
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[UserRoles]') AND type IN ('U'))
	DROP TABLE [dbo].[UserRoles]
GO

CREATE TABLE [dbo].[UserRoles] (
  [UserID] int  NOT NULL,
  [RoleID] int  NOT NULL
)
GO

ALTER TABLE [dbo].[UserRoles] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of UserRoles
-- ----------------------------
INSERT INTO [dbo].[UserRoles] ([UserID], [RoleID]) VALUES (N'1', N'1')
GO

INSERT INTO [dbo].[UserRoles] ([UserID], [RoleID]) VALUES (N'4', N'2')
GO


-- ----------------------------
-- Table structure for Users
-- ----------------------------
IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type IN ('U'))
	DROP TABLE [dbo].[Users]
GO

CREATE TABLE [dbo].[Users] (
  [UserID] int  IDENTITY(1,1) NOT NULL,
  [UserName] varchar(50) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [PasswordHash] varchar(256) COLLATE Chinese_PRC_CI_AS  NOT NULL,
  [Enable] bit DEFAULT 1 NULL
)
GO

ALTER TABLE [dbo].[Users] SET (LOCK_ESCALATION = TABLE)
GO


-- ----------------------------
-- Records of Users
-- ----------------------------
SET IDENTITY_INSERT [dbo].[Users] ON
GO

INSERT INTO [dbo].[Users] ([UserID], [UserName], [PasswordHash], [Enable]) VALUES (N'1', N'1', N'6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b', N'1')
GO

INSERT INTO [dbo].[Users] ([UserID], [UserName], [PasswordHash], [Enable]) VALUES (N'4', N'2', N'6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b', N'1')
GO

SET IDENTITY_INSERT [dbo].[Users] OFF
GO


-- ----------------------------
-- Primary Key structure for table Permissions
-- ----------------------------
ALTER TABLE [dbo].[Permissions] ADD CONSTRAINT [PK__Permissi__91FE5751239E4DCF] PRIMARY KEY CLUSTERED ([PermissionCode])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Primary Key structure for table RolePermissions
-- ----------------------------
ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [PK__RolePerm__83E52B4F2F10007B] PRIMARY KEY CLUSTERED ([RoleID], [PermissionCode])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for Roles
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[Roles]', RESEED, 2)
GO


-- ----------------------------
-- Uniques structure for table Roles
-- ----------------------------
ALTER TABLE [dbo].[Roles] ADD CONSTRAINT [UQ__Roles__8A2B61601FCDBCEB] UNIQUE NONCLUSTERED ([RoleName] ASC)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Primary Key structure for table Roles
-- ----------------------------
ALTER TABLE [dbo].[Roles] ADD CONSTRAINT [PK__Roles__8AFACE3A1CF15040] PRIMARY KEY CLUSTERED ([RoleID])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Primary Key structure for table UserRoles
-- ----------------------------
ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [PK__UserRole__AF27604F29572725] PRIMARY KEY CLUSTERED ([UserID], [RoleID])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Auto increment value for Users
-- ----------------------------
DBCC CHECKIDENT ('[dbo].[Users]', RESEED, 4)
GO


-- ----------------------------
-- Uniques structure for table Users
-- ----------------------------
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [UQ__Users__C9F28456182C9B23] UNIQUE NONCLUSTERED ([UserName] ASC)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Primary Key structure for table Users
-- ----------------------------
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [PK__Users__1788CCAC15502E78] PRIMARY KEY CLUSTERED ([UserID])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)  
ON [PRIMARY]
GO


-- ----------------------------
-- Foreign Keys structure for table Permissions
-- ----------------------------
ALTER TABLE [dbo].[Permissions] ADD CONSTRAINT [FK__Permissio__Paren__267ABA7A] FOREIGN KEY ([ParentCode]) REFERENCES [dbo].[Permissions] ([PermissionCode]) ON DELETE NO ACTION ON UPDATE NO ACTION
GO


-- ----------------------------
-- Foreign Keys structure for table RolePermissions
-- ----------------------------
ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [FK__RolePermi__RoleI__30F848ED] FOREIGN KEY ([RoleID]) REFERENCES [dbo].[Roles] ([RoleID]) ON DELETE NO ACTION ON UPDATE NO ACTION
GO

ALTER TABLE [dbo].[RolePermissions] ADD CONSTRAINT [FK__RolePermi__Permi__31EC6D26] FOREIGN KEY ([PermissionCode]) REFERENCES [dbo].[Permissions] ([PermissionCode]) ON DELETE NO ACTION ON UPDATE NO ACTION
GO


-- ----------------------------
-- Foreign Keys structure for table UserRoles
-- ----------------------------
ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK__UserRoles__UserI__2B3F6F97] FOREIGN KEY ([UserID]) REFERENCES [dbo].[Users] ([UserID]) ON DELETE NO ACTION ON UPDATE NO ACTION
GO

ALTER TABLE [dbo].[UserRoles] ADD CONSTRAINT [FK__UserRoles__RoleI__2C3393D0] FOREIGN KEY ([RoleID]) REFERENCES [dbo].[Roles] ([RoleID]) ON DELETE NO ACTION ON UPDATE NO ACTION
GO

