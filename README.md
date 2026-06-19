# ChatSystem

一个基于 .NET 的实时聊天系统，包含 ASP.NET Core 服务端和 WPF 桌面客户端。

## 项目结构

```
ChatSystem/
├── ChatSystem.Core/          # 核心类库 — 数据模型、DTO、枚举
│   ├── Models/               # 实体模型 (User, Friend, Group, Message...)
│   ├── DTOs/                 # 数据传输对象
│   └── Enums/                # 枚举 (UserRole, MessageType...)
├── ChatSystem.Data/          # 数据访问层 — EF Core + Repository 模式
│   ├── Repositories/         # 仓储接口与实现
│   ├── Migrations/           # EF Core 数据库迁移
│   └── ChatSystemDbContext.cs
├── ChatSystem.Server/        # ASP.NET Core 服务端
│   ├── Controllers/Api/      # REST API 控制器
│   ├── Controllers/Mvc/      # MVC 页面控制器
│   ├── Hubs/                 # SignalR 实时通信 Hub
│   ├── Helpers/              # JWT 工具类
│   ├── Views/                # Razor 视图
│   └── wwwroot/              # 静态资源 (CSS/JS/lib)
├── ChatSystem.Wpf/           # WPF 桌面客户端 (MVVM)
│   ├── Views/                # XAML 窗口
│   ├── ViewModels/           # 视图模型
│   ├── Services/             # API/SignalR 服务
│   └── Converters/           # 值转换器
└── ChatSystem.sln
```

## 功能特性

### 用户系统
- 🔐 JWT 登录/注册，密码 BCrypt 加密
- 👑 角色权限（管理员 / 普通用户）
- ✅ 注册审核机制（待审核 → 正常 / 封禁）

### 聊天功能
- 💬 **私聊** — 好友间一对一实时消息
- 👥 **群聊** — 创建群组、群消息、邀请成员
- 📎 **文件上传** — 私聊和群聊均支持文件传输
- 😊 **表情包** — 5 类 80 个 Emoji，点击即插入
- 🔴 **未读红点** — 未读消息数红色徽章，桌面端和 Web 端同步
- ⌨️ **Enter 发送** — Enter 发消息，Shift+Enter 换行

### 好友系统
- 🔍 搜索用户、发送好友申请
- ✅ 同意/拒绝好友申请
- 🗑️ 删除好友
- 📡 实时通知（好友上线/下线、申请推送）

### 群组管理
- 👑 **群主解散群聊**（仅创建者可解散）
- 🚪 **成员退出群聊**（非群主可退出）
- ➕ **邀请成员**（批量勾选好友入群）
- 📡 实时通知（成员加入/移除、群解散）

### 聊天记录
- 📋 关键词实时搜索（输入即搜，无需按 Enter）
- 📅 日期范围筛选
- 👤 显示发送人/接收人标记（发送给XXX / 来自XXX / 在XXX群中 XXX说）
- 🗑️ 自己发送的消息可删除

### 双端支持
- 🌐 **Web 端** — ASP.NET MVC + Bootstrap 5 + Vanilla JS
- 🖥️ **桌面端** — WPF MVVM 架构，与 Web 端功能对齐
- 📡 **实时通信** — 基于 SignalR 的 WebSocket 推送
- 📖 **Swagger** — 开发环境自动生成 API 文档

## 技术栈

| 层级 | 技术 |
|------|------|
| 后端框架 | ASP.NET Core (.NET 8) |
| 实时通信 | SignalR |
| 认证 | JWT (JSON Web Token) |
| 数据库 | MySQL + Entity Framework Core |
| ORM | Pomelo.EntityFrameworkCore.MySql |
| 密码加密 | BCrypt.Net-Next |
| Web 前端 | Razor + Bootstrap 5 + Vanilla JS |
| 桌面客户端 | WPF (.NET 8) + MVVM |
| API 文档 | Swagger / OpenAPI |

## 快速开始

### 环境要求

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL 5.7+](https://dev.mysql.com/downloads/)
- Visual Studio 2022+ 或 VS Code

### 1. 配置数据库

在 `ChatSystem.Server/appsettings.json` 中修改数据库连接字符串：

```json
{
  "ConnectionStrings": {
    "Default": "server=localhost;port=3306;database=chatsystem;user=root;password=你的密码;charset=utf8mb4"
  }
}
```

### 2. 初始化数据库

```bash
# 还原 NuGet 包
dotnet restore

# 执行 EF Core 迁移建表
dotnet ef database update --project ChatSystem.Data --startup-project ChatSystem.Server

# 或直接导入 SQL 文件（含测试数据）
# mysql -u root -p chatsystem < chatsystem.sql
```

### 3. 启动服务端

```bash
dotnet run --project ChatSystem.Server/ChatSystem.Server.csproj
```

启动后访问：
- 🌐 Web 页面：`http://localhost:5136`
- 💬 聊天界面：`http://localhost:5136/chat`
- 👥 好友管理：`http://localhost:5136/chat/friends`
- 📋 聊天记录：`http://localhost:5136/chat/history`
- 📖 Swagger API：`http://localhost:5136/swagger`

### 4. 启动桌面客户端

```bash
dotnet run --project ChatSystem.Wpf/ChatSystem.Wpf.csproj
```

### 5. 测试账号

SQL 导入后可用以下测试账号（密码请咨询项目组成员）：

| 用户名 | 昵称 | 角色 |
|--------|------|------|
| `admin` | 管理员 | 管理员 |
| `zhangsan` | 张三 | 用户 |
| `lisi` | 李四 | 用户 |
| `wangwu` | 王五 | 用户 |
| `zhaoliu` | 赵六 | 用户 |

## API 概览

| 路由 | 说明 |
|------|------|
| `POST /api/auth/login` | 用户登录 |
| `POST /api/auth/register` | 用户注册 |
| `GET /api/auth/me` | 获取当前用户信息 |
| `GET /api/users/search` | 搜索用户 |
| `GET /api/friends` | 获取好友列表 |
| `POST /api/friends/request` | 发送好友请求 |
| `GET /api/friends/requests/pending` | 待处理的好友申请 |
| `POST /api/friends/requests/{id}/accept` | 同意好友申请 |
| `POST /api/friends/requests/{id}/reject` | 拒绝好友申请 |
| `DELETE /api/friends/{id}` | 删除好友 |
| `GET /api/messages/private/{userId}` | 获取私聊消息 |
| `POST /api/messages/file` | 上传私聊文件 |
| `GET /api/messages/search` | 搜索私聊消息 |
| `GET /api/messages/history` | 合并搜索聊天记录（私聊+群聊） |
| `DELETE /api/messages/{id}` | 删除私聊消息 |
| `GET /api/groups` | 获取我的群组 |
| `POST /api/groups` | 创建群组 |
| `DELETE /api/groups/{id}` | 解散群组（仅群主） |
| `POST /api/groups/{id}/members` | 添加群成员 |
| `DELETE /api/groups/{id}/members/{userId}` | 移除/退出群成员 |
| `POST /api/groups/file` | 上传群聊文件 |
| `DELETE /api/groups/{groupId}/messages/{messageId}` | 删除群消息 |
| `GET /api/admin/users/pending` | 管理员 - 待审核用户 |
| `POST /api/admin/users/{id}/approve` | 管理员 - 审核通过 |
| `POST /api/admin/users/{id}/ban` | 管理员 - 封禁用户 |
| `GET /api/admin/messages` | 管理员 - 全站消息搜索 |

> SignalR Hub 端点：`/hubs/chat`

## 项目架构

```
┌──────────────────────────────────────────┐
│            ChatSystem.Wpf                │
│         (WPF 桌面客户端)                   │
│    MVVM → ApiService + SignalRService    │
└──────────────┬───────┬───────────────────┘
               │ HTTP  │ WebSocket
               ▼       ▼
┌──────────────────────────────────────────┐
│          ChatSystem.Server               │
│       (ASP.NET Core 服务端)               │
│  ┌─────────┐  ┌──────────┐  ┌─────────┐ │
│  │ REST API│  │ MVC Views│  │SignalR Hub│ │
│  └────┬────┘  └────┬─────┘  └────┬─────┘ │
│       └────────────┼─────────────┘       │
│                    ▼                     │
│  ┌────────────────────────────────────┐  │
│  │        ChatSystem.Data              │ │
│  │    Repository + DbContext           │ │
│  └────────────────┬───────────────────┘  │
└───────────────────┼──────────────────────┘
                    ▼
┌──────────────────────────────────────────┐
│              MySQL 数据库                  │
└──────────────────────────────────────────┘
```

## JWT 配置

在 `appsettings.json` 中修改 JWT 设置：

```json
{
  "Jwt": {
    "Key": "你的密钥-至少32个字符-越长越好!!",
    "Issuer": "ChatSystem",
    "Audience": "ChatSystemClient",
    "ExpireHours": 24
  }
}
```

## License

MIT
