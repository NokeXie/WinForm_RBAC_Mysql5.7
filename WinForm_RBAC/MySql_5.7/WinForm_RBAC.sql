/*
 Navicat Premium Dump SQL

 Source Server         : 192.168.10.244
 Source Server Type    : MySQL
 Source Server Version : 50744 (5.7.44)
 Source Host           : 192.168.10.244:3306
 Source Schema         : WinForm_RBAC

 Target Server Type    : MySQL
 Target Server Version : 50744 (5.7.44)
 File Encoding         : 65001

 Date: 19/03/2026 08:09:51
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for Permissions
-- ----------------------------
DROP TABLE IF EXISTS `Permissions`;
CREATE TABLE `Permissions`  (
  `PermissionCode` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `ParentCode` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `Description` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
  `SortOrder` int(11) NULL DEFAULT 0,
  PRIMARY KEY (`PermissionCode`) USING BTREE,
  INDEX `FK_Permissions_Parent`(`ParentCode`) USING BTREE,
  CONSTRAINT `FK_Permissions_Parent` FOREIGN KEY (`ParentCode`) REFERENCES `Permissions` (`PermissionCode`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of Permissions
-- ----------------------------
INSERT INTO `Permissions` VALUES ('SystemManger', NULL, '系统管理', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.PassWord', 'SystemManger', '修改密码', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.RoleManger', 'SystemManger', '角色管理', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.RoleManger.Add', 'SystemManger.RoleManger', '新增角色', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.RoleManger.Delete', 'SystemManger.RoleManger', '删除角色', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.RoleManger.NameEdit', 'SystemManger.RoleManger', '角色名称修改', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.RoleManger.Refresh_Permission', 'SystemManger.RoleManger', '刷新权限模块', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.RoleManger.Save', 'SystemManger.RoleManger', '保存角色', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.UserManger', 'SystemManger', '用户管理', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.UserManger.Add', 'SystemManger.UserManger', '新增用户', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.UserManger.Delete', 'SystemManger.UserManger', '删除用户', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.UserManger.Disable', 'SystemManger.UserManger', '禁用用户', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.UserManger.Edit', 'SystemManger.UserManger', '编辑用户', 0);
INSERT INTO `Permissions` VALUES ('SystemManger.UserManger.Enable', 'SystemManger.UserManger', '启用用户', 0);

-- ----------------------------
-- Table structure for RolePermissions
-- ----------------------------
DROP TABLE IF EXISTS `RolePermissions`;
CREATE TABLE `RolePermissions`  (
  `RoleID` int(11) NOT NULL,
  `PermissionCode` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`RoleID`, `PermissionCode`) USING BTREE,
  CONSTRAINT `FK_RolePermissions_RoleID` FOREIGN KEY (`RoleID`) REFERENCES `Roles` (`RoleID`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of RolePermissions
-- ----------------------------
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.PassWord');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.RoleManger');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.RoleManger.Add');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.RoleManger.Delete');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.RoleManger.NameEdit');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.RoleManger.Refresh_Permission');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.RoleManger.Save');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.UserManger');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.UserManger.Add');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.UserManger.Delete');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.UserManger.Disable');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.UserManger.Edit');
INSERT INTO `RolePermissions` VALUES (24, 'SystemManger.UserManger.Enable');

-- ----------------------------
-- Table structure for Roles
-- ----------------------------
DROP TABLE IF EXISTS `Roles`;
CREATE TABLE `Roles`  (
  `RoleID` int(11) NOT NULL AUTO_INCREMENT,
  `RoleName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  PRIMARY KEY (`RoleID`) USING BTREE,
  UNIQUE INDEX `UQ_RoleName`(`RoleName`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 25 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of Roles
-- ----------------------------
INSERT INTO `Roles` VALUES (24, '管理员');

-- ----------------------------
-- Table structure for UserRoles
-- ----------------------------
DROP TABLE IF EXISTS `UserRoles`;
CREATE TABLE `UserRoles`  (
  `UserID` int(11) NOT NULL,
  `RoleID` int(11) NOT NULL,
  PRIMARY KEY (`UserID`, `RoleID`) USING BTREE,
  INDEX `FK_UserRoles_RoleID`(`RoleID`) USING BTREE,
  CONSTRAINT `FK_UserRoles_RoleID` FOREIGN KEY (`RoleID`) REFERENCES `Roles` (`RoleID`) ON DELETE NO ACTION ON UPDATE NO ACTION,
  CONSTRAINT `FK_UserRoles_UserID` FOREIGN KEY (`UserID`) REFERENCES `Users` (`UserID`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of UserRoles
-- ----------------------------
INSERT INTO `UserRoles` VALUES (31, 24);

-- ----------------------------
-- Table structure for Users
-- ----------------------------
DROP TABLE IF EXISTS `Users`;
CREATE TABLE `Users`  (
  `UserID` int(11) NOT NULL AUTO_INCREMENT,
  `UserName` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `PasswordHash` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `Enable` tinyint(1) NULL DEFAULT 1,
  PRIMARY KEY (`UserID`) USING BTREE,
  UNIQUE INDEX `UQ_UserName`(`UserName`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 32 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of Users
-- ----------------------------
INSERT INTO `Users` VALUES (7, '张三', '111111', 1);
INSERT INTO `Users` VALUES (8, 'company_a1', 'company_a1', 1);
INSERT INTO `Users` VALUES (31, '1', '6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b', 1);

SET FOREIGN_KEY_CHECKS = 1;
