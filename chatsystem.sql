/*
 Navicat Premium Dump SQL

 Source Server         : clubManage
 Source Server Type    : MySQL
 Source Server Version : 80404 (8.4.4)
 Source Host           : localhost:3306
 Source Schema         : chatsystem

 Target Server Type    : MySQL
 Target Server Version : 80404 (8.4.4)
 File Encoding         : 65001

 Date: 19/06/2026 15:55:56
*/

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for __efmigrationshistory
-- ----------------------------
DROP TABLE IF EXISTS `__efmigrationshistory`;
CREATE TABLE `__efmigrationshistory`  (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of __efmigrationshistory
-- ----------------------------
INSERT INTO `__efmigrationshistory` VALUES ('20260619041425_InitialCreate', '8.0.0');
INSERT INTO `__efmigrationshistory` VALUES ('20260619075021_AddGroupMessageFileSupport', '8.0.0');

-- ----------------------------
-- Table structure for friendrequests
-- ----------------------------
DROP TABLE IF EXISTS `friendrequests`;
CREATE TABLE `friendrequests`  (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FromUserId` int NOT NULL,
  `ToUserId` int NOT NULL,
  `Status` int NOT NULL,
  `SentAt` datetime(6) NOT NULL,
  `RespondedAt` datetime(6) NULL DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_FriendRequests_FromUserId_ToUserId`(`FromUserId` ASC, `ToUserId` ASC) USING BTREE,
  INDEX `IX_FriendRequests_ToUserId_Status`(`ToUserId` ASC, `Status` ASC) USING BTREE,
  CONSTRAINT `FK_FriendRequests_Users_FromUserId` FOREIGN KEY (`FromUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `FK_FriendRequests_Users_ToUserId` FOREIGN KEY (`ToUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 4 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of friendrequests
-- ----------------------------
INSERT INTO `friendrequests` VALUES (2, 28, 27, 0, '2026-06-19 20:32:02.000000', NULL);
INSERT INTO `friendrequests` VALUES (3, 30, 26, 0, '2026-06-19 14:43:06.280762', NULL);

-- ----------------------------
-- Table structure for friends
-- ----------------------------
DROP TABLE IF EXISTS `friends`;
CREATE TABLE `friends`  (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `FriendUserId` int NOT NULL,
  `AddedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `IX_Friends_UserId_FriendUserId`(`UserId` ASC, `FriendUserId` ASC) USING BTREE,
  INDEX `IX_Friends_FriendUserId`(`FriendUserId` ASC) USING BTREE,
  CONSTRAINT `FK_Friends_Users_FriendUserId` FOREIGN KEY (`FriendUserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Friends_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 25 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of friends
-- ----------------------------
INSERT INTO `friends` VALUES (17, 26, 27, '2026-06-19 20:32:02.000000');
INSERT INTO `friends` VALUES (18, 27, 26, '2026-06-19 20:32:02.000000');
INSERT INTO `friends` VALUES (19, 26, 28, '2026-06-19 20:32:02.000000');
INSERT INTO `friends` VALUES (20, 28, 26, '2026-06-19 20:32:02.000000');
INSERT INTO `friends` VALUES (21, 26, 29, '2026-06-19 20:32:02.000000');
INSERT INTO `friends` VALUES (22, 29, 26, '2026-06-19 20:32:02.000000');
INSERT INTO `friends` VALUES (23, 29, 30, '2026-06-19 20:32:02.000000');
INSERT INTO `friends` VALUES (24, 30, 29, '2026-06-19 20:32:02.000000');

-- ----------------------------
-- Table structure for groupmembers
-- ----------------------------
DROP TABLE IF EXISTS `groupmembers`;
CREATE TABLE `groupmembers`  (
  `Id` int NOT NULL AUTO_INCREMENT,
  `GroupId` int NOT NULL,
  `UserId` int NOT NULL,
  `JoinedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `IX_GroupMembers_GroupId_UserId`(`GroupId` ASC, `UserId` ASC) USING BTREE,
  INDEX `IX_GroupMembers_UserId`(`UserId` ASC) USING BTREE,
  CONSTRAINT `FK_GroupMembers_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `FK_GroupMembers_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 20 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of groupmembers
-- ----------------------------
INSERT INTO `groupmembers` VALUES (6, 3, 26, '2026-06-19 20:32:02.000000');
INSERT INTO `groupmembers` VALUES (7, 3, 27, '2026-06-19 20:32:02.000000');
INSERT INTO `groupmembers` VALUES (8, 3, 28, '2026-06-19 20:32:02.000000');
INSERT INTO `groupmembers` VALUES (9, 4, 29, '2026-06-19 20:32:02.000000');
INSERT INTO `groupmembers` VALUES (10, 4, 30, '2026-06-19 20:32:02.000000');
INSERT INTO `groupmembers` VALUES (18, 7, 26, '2026-06-19 15:15:51.727951');
INSERT INTO `groupmembers` VALUES (19, 7, 28, '2026-06-19 15:15:55.836059');

-- ----------------------------
-- Table structure for groupmessages
-- ----------------------------
DROP TABLE IF EXISTS `groupmessages`;
CREATE TABLE `groupmessages`  (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `GroupId` int NOT NULL,
  `SenderId` int NOT NULL,
  `Content` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MessageType` int NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  `SentAt` datetime(6) NOT NULL,
  `FileName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `FilePath` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_GroupMessages_GroupId_SentAt`(`GroupId` ASC, `SentAt` ASC) USING BTREE,
  INDEX `IX_GroupMessages_SenderId`(`SenderId` ASC) USING BTREE,
  CONSTRAINT `FK_GroupMessages_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `FK_GroupMessages_Users_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 18 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of groupmessages
-- ----------------------------
INSERT INTO `groupmessages` VALUES (1, 3, 26, '大家早上好！', 0, 0, '2026-06-18 17:00:00.000000', NULL, NULL);
INSERT INTO `groupmessages` VALUES (2, 3, 27, '早！项目进度怎么样了？', 0, 0, '2026-06-18 17:30:00.000000', NULL, NULL);
INSERT INTO `groupmessages` VALUES (3, 3, 26, '前端部分基本完成了', 0, 0, '2026-06-18 17:35:00.000000', NULL, NULL);
INSERT INTO `groupmessages` VALUES (4, 3, 28, '后端API也差不多了，就差联调了', 0, 0, '2026-06-18 17:40:00.000000', NULL, NULL);
INSERT INTO `groupmessages` VALUES (5, 4, 29, '最近有人学Blazor吗？', 0, 0, '2026-06-18 23:00:00.000000', NULL, NULL);
INSERT INTO `groupmessages` VALUES (6, 4, 30, '我在学，感觉挺有意思的', 0, 0, '2026-06-18 23:20:00.000000', NULL, NULL);
INSERT INTO `groupmessages` VALUES (7, 4, 29, '比WPF怎么样？', 0, 0, '2026-06-18 23:30:00.000000', NULL, NULL);
INSERT INTO `groupmessages` VALUES (8, 3, 26, 'ok', 0, 0, '2026-06-19 14:48:56.943664', NULL, NULL);
INSERT INTO `groupmessages` VALUES (13, 3, 26, '111', 0, 1, '2026-06-19 15:13:47.741713', NULL, NULL);
INSERT INTO `groupmessages` VALUES (15, 3, 26, 'test', 0, 1, '2026-06-19 15:14:23.918309', NULL, NULL);
INSERT INTO `groupmessages` VALUES (16, 7, 26, '111', 0, 0, '2026-06-19 15:15:59.190811', NULL, NULL);
INSERT INTO `groupmessages` VALUES (17, 3, 26, '[文件] 308c488766200e736dc49faeb83c4cda1340190821.jpg', 1, 0, '2026-06-19 15:53:59.167248', '308c488766200e736dc49faeb83c4cda1340190821.jpg', '/uploads/48b79761-6b89-40d2-b3fb-e690c68e14aa_308c488766200e736dc49faeb83c4cda1340190821.jpg');

-- ----------------------------
-- Table structure for groups
-- ----------------------------
DROP TABLE IF EXISTS `groups`;
CREATE TABLE `groups`  (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatorId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_Groups_CreatorId`(`CreatorId` ASC) USING BTREE,
  CONSTRAINT `FK_Groups_Users_CreatorId` FOREIGN KEY (`CreatorId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 8 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of groups
-- ----------------------------
INSERT INTO `groups` VALUES (3, '项目讨论群', 26, '2026-06-19 20:32:02.000000');
INSERT INTO `groups` VALUES (4, '技术交流群', 29, '2026-06-19 20:32:02.000000');
INSERT INTO `groups` VALUES (7, 'test', 26, '2026-06-19 15:15:51.714079');

-- ----------------------------
-- Table structure for privatemessages
-- ----------------------------
DROP TABLE IF EXISTS `privatemessages`;
CREATE TABLE `privatemessages`  (
  `Id` bigint NOT NULL AUTO_INCREMENT,
  `SenderId` int NOT NULL,
  `ReceiverId` int NOT NULL,
  `Content` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `MessageType` int NOT NULL,
  `FileName` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `FilePath` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
  `SentAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  INDEX `IX_PrivateMessages_ReceiverId`(`ReceiverId` ASC) USING BTREE,
  INDEX `IX_PrivateMessages_SenderId_ReceiverId_SentAt`(`SenderId` ASC, `ReceiverId` ASC, `SentAt` ASC) USING BTREE,
  CONSTRAINT `FK_PrivateMessages_Users_ReceiverId` FOREIGN KEY (`ReceiverId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_PrivateMessages_Users_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE = InnoDB AUTO_INCREMENT = 29 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of privatemessages
-- ----------------------------
INSERT INTO `privatemessages` VALUES (1, 26, 27, '你好李四！', 0, NULL, NULL, 0, '2026-06-18 18:00:00.000000');
INSERT INTO `privatemessages` VALUES (2, 27, 26, '嗨张三，最近怎么样？', 0, NULL, NULL, 0, '2026-06-18 18:05:00.000000');
INSERT INTO `privatemessages` VALUES (3, 26, 27, '挺好的，在写项目报告', 0, NULL, NULL, 0, '2026-06-18 18:10:00.000000');
INSERT INTO `privatemessages` VALUES (4, 27, 26, '加油！需要帮忙的话说一声', 0, NULL, NULL, 0, '2026-06-18 18:15:00.000000');
INSERT INTO `privatemessages` VALUES (5, 26, 28, '王五，明天开会别忘了', 0, NULL, NULL, 0, '2026-06-18 19:00:00.000000');
INSERT INTO `privatemessages` VALUES (6, 28, 26, '收到，几点？', 0, NULL, NULL, 0, '2026-06-18 19:02:00.000000');
INSERT INTO `privatemessages` VALUES (7, 26, 28, '下午3点，三楼会议室', 0, NULL, NULL, 1, '2026-06-18 19:05:00.000000');
INSERT INTO `privatemessages` VALUES (8, 29, 26, '张三，听说你最近在学习.NET？', 0, NULL, NULL, 0, '2026-06-18 22:00:00.000000');
INSERT INTO `privatemessages` VALUES (9, 26, 29, '是的，在做一个聊天系统的项目', 0, NULL, NULL, 1, '2026-06-18 22:10:00.000000');
INSERT INTO `privatemessages` VALUES (10, 29, 26, '厉害啊，有机会交流一下', 0, NULL, NULL, 0, '2026-06-18 22:15:00.000000');
INSERT INTO `privatemessages` VALUES (11, 26, 27, '1234556', 0, NULL, NULL, 1, '2026-06-19 00:00:00.000000');
INSERT INTO `privatemessages` VALUES (12, 26, 28, '111', 0, NULL, NULL, 0, '2026-06-19 00:05:00.000000');
INSERT INTO `privatemessages` VALUES (13, 26, 29, '11111111', 0, NULL, NULL, 0, '2026-06-19 00:10:00.000000');
INSERT INTO `privatemessages` VALUES (14, 26, 27, '111', 0, NULL, NULL, 1, '2026-06-19 14:33:38.203283');
INSERT INTO `privatemessages` VALUES (15, 27, 26, '111232312321312', 0, NULL, NULL, 0, '2026-06-19 14:33:42.689191');
INSERT INTO `privatemessages` VALUES (16, 26, 27, '1234566', 0, NULL, NULL, 1, '2026-06-19 14:35:01.313926');
INSERT INTO `privatemessages` VALUES (17, 26, 27, '测试', 0, NULL, NULL, 1, '2026-06-19 14:36:22.944070');
INSERT INTO `privatemessages` VALUES (18, 26, 27, 'test1', 0, NULL, NULL, 1, '2026-06-19 14:41:42.973486');
INSERT INTO `privatemessages` VALUES (19, 26, 27, '11111', 0, NULL, NULL, 1, '2026-06-19 14:42:25.241656');
INSERT INTO `privatemessages` VALUES (20, 26, 27, '测试', 0, NULL, NULL, 1, '2026-06-19 14:42:27.602998');
INSERT INTO `privatemessages` VALUES (21, 26, 27, '测试', 0, NULL, NULL, 1, '2026-06-19 15:04:54.324703');
INSERT INTO `privatemessages` VALUES (22, 26, 27, '测试', 0, NULL, NULL, 0, '2026-06-19 15:27:22.677942');
INSERT INTO `privatemessages` VALUES (26, 26, 27, '[文件] 26_05_02_10_54_48.png', 1, '26_05_02_10_54_48.png', '/uploads/ea1e3660-9a87-48d8-868f-fe5d2133727e_26_05_02_10_54_48.png', 0, '2026-06-19 15:43:43.032509');
INSERT INTO `privatemessages` VALUES (27, 26, 27, '[文件] 1265701.png', 1, '1265701.png', '/uploads/59d62e68-64c3-411a-b451-e901f3cdc199_1265701.png', 0, '2026-06-19 15:44:41.093633');
INSERT INTO `privatemessages` VALUES (28, 26, 27, '[文件] 7eb283de9c82d158fa4611fd970a19d8be3e42f5.jpg', 1, '7eb283de9c82d158fa4611fd970a19d8be3e42f5.jpg', '/uploads/3f071969-26bb-42f6-b900-948ec5ffedcb_7eb283de9c82d158fa4611fd970a19d8be3e42f5.jpg', 0, '2026-06-19 15:47:14.698200');

-- ----------------------------
-- Table structure for users
-- ----------------------------
DROP TABLE IF EXISTS `users`;
CREATE TABLE `users`  (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PasswordHash` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Nickname` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Role` int NOT NULL DEFAULT 0,
  `Status` int NOT NULL DEFAULT 0,
  `CreatedAt` datetime(6) NOT NULL,
  `LastLoginAt` datetime(6) NULL DEFAULT NULL,
  `AvatarPath` varchar(256) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NULL DEFAULT NULL,
  PRIMARY KEY (`Id`) USING BTREE,
  UNIQUE INDEX `IX_Users_Username`(`Username` ASC) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 34 CHARACTER SET = utf8mb4 COLLATE = utf8mb4_0900_ai_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Records of users
-- ----------------------------
INSERT INTO `users` VALUES (25, 'admin', '$2a$11$5AO0vMa0E0G2SiEbpVQhZuGPk2iqiw5ko9G1V7twhQlNKSDr36PVm', '管理员', 1, 1, '2026-06-19 20:32:02.000000', '2026-06-19 15:54:14.314997', NULL);
INSERT INTO `users` VALUES (26, 'zhangsan', '$2a$11$5AO0vMa0E0G2SiEbpVQhZuGPk2iqiw5ko9G1V7twhQlNKSDr36PVm', '张三', 0, 1, '2026-06-19 20:32:02.000000', '2026-06-19 15:53:46.423828', NULL);
INSERT INTO `users` VALUES (27, 'lisi', '$2a$11$5AO0vMa0E0G2SiEbpVQhZuGPk2iqiw5ko9G1V7twhQlNKSDr36PVm', '李四', 0, 1, '2026-06-19 20:32:02.000000', '2026-06-19 15:42:46.734854', NULL);
INSERT INTO `users` VALUES (28, 'wangwu', '$2a$11$5AO0vMa0E0G2SiEbpVQhZuGPk2iqiw5ko9G1V7twhQlNKSDr36PVm', '王五', 0, 1, '2026-06-19 20:32:02.000000', NULL, NULL);
INSERT INTO `users` VALUES (29, 'zhaoliu', '$2a$11$5AO0vMa0E0G2SiEbpVQhZuGPk2iqiw5ko9G1V7twhQlNKSDr36PVm', '赵六', 0, 1, '2026-06-19 20:32:02.000000', NULL, NULL);
INSERT INTO `users` VALUES (30, 'sunqi', '$2a$11$5AO0vMa0E0G2SiEbpVQhZuGPk2iqiw5ko9G1V7twhQlNKSDr36PVm', '孙七', 0, 2, '2026-06-19 20:32:02.000000', '2026-06-19 14:42:56.797059', NULL);
INSERT INTO `users` VALUES (31, 'pending1', '$2a$11$5AO0vMa0E0G2SiEbpVQhZuGPk2iqiw5ko9G1V7twhQlNKSDr36PVm', '待审核用户1', 0, 0, '2026-06-19 20:32:02.000000', NULL, NULL);
INSERT INTO `users` VALUES (33, 'luoxi', '$2a$11$YLfXsV3fatmOymFGb1gpxeQfg4GdAi//zYvhk7V9Yy5XTiyhH62/O', '落兮ya', 0, 1, '2026-06-19 15:09:02.722155', '2026-06-19 15:09:40.764370', NULL);

SET FOREIGN_KEY_CHECKS = 1;
