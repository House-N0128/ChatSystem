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
│   └── ChatSystemDbContext.cs
├── ChatSystem.Server/        # ASP.NET Core 服务端
│   ├── Controllers/Api/      # REST API 控制器
│   ├── Controllers/Mvc/      # MVC 页面控制器
│   ├── Hubs/                 # SignalR 实时通信 Hub
│   ├── Helpers/              # JWT 工具类
│   └── Views/                # Razor 视图
├── ChatSystem.Wpf/           # WPF 桌面客户端 (MVVM)
│   ├── Views/                # XAML 窗口
│   ├── ViewModels/           # 视图模型
│   ├── Services/             # API/SignalR 服务
│   └── Converters/           # 值转换器
└── ChatSystem.sln
```

## 功能特性

- 🔐 **用户认证** — JWT 登录/注册，角色权限（管理员 / 普通用户）
- 💬 **私聊** — 好友间一对一实时消息
- 👥 **群聊** — 创建群组、群消息
- 👫 **好友系统** — 添加好友、好友请求处理
- 📎 **文件上传** — 支持消息附件上传
- 📡 **实时通信** — 基于 SignalR 的 WebSocket 推送
- 🌐 **Web 端** — ASP.NET MVC 页面
- 🖥️ **桌面端** — WPF 原生客户端（MVVM 架构）
- 📖 **Swagger** — 开发环境自动生成 API 文档

## 技术栈

| 层级 | 技术 |
|------|------|
| 后端框架 | ASP.NET Core (.NET 8+) |
| 实时通信 | SignalR |
| 认证 | JWT (JSON Web Token) |
| 数据库 | MySQL + Entity Framework Core |
| ORM | Pomelo.EntityFrameworkCore.MySql |
| Web 前端 | Razor + Bootstrap 5 + jQuery |
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

### 2. 还原依赖并运行

```bash
# 还原 NuGet 包
dotnet restore

# 运行服务端
dotnet run --project ChatSystem.Server/ChatSystem.Server.csproj
```

服务端启动后访问：
- 🌐 Web 页面：`https://localhost:5001`
- 📖 Swagger API：`https://localhost:5001/swagger`

### 3. 运行 WPF 客户端

在 Visual Studio 中将 `ChatSystem.Wpf` 设为启动项目，或：

```bash
dotnet run --project ChatSystem.Wpf/ChatSystem.Wpf.csproj
```

## API 概览

| 路由 | 说明 |
|------|------|
| `POST /api/auth/login` | 用户登录 |
| `POST /api/auth/register` | 用户注册 |
| `GET /api/users` | 获取用户列表 |
| `GET /api/friends` | 获取好友列表 |
| `POST /api/friends/request` | 发送好友请求 |
| `GET /api/messages` | 获取消息 |
| `POST /api/messages` | 发送消息 |
| `GET /api/groups` | 获取群组列表 |
| `POST /api/groups` | 创建群组 |
| `GET /api/admin/users` | 管理员 - 用户管理 |

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
