# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Restore NuGet packages (entire solution)
dotnet restore

# Build the solution
dotnet build

# Run the ASP.NET Core server (REST API + SignalR + MVC pages)
dotnet run --project ChatSystem.Server/ChatSystem.Server.csproj

# Run the WPF desktop client (requires a running server)
dotnet run --project ChatSystem.Wpf/ChatSystem.Wpf.csproj
```

Server starts on `http://localhost:5136` by default, Swagger UI at `/swagger`.

## Database Setup

The project uses MySQL with EF Core (Pomelo provider). The connection string is in `ChatSystem.Server/appsettings.json`:

```json
"Default": "server=localhost;port=3306;database=chatsystem;user=root;password=123456;charset=utf8mb4"
```

EF Core migrations are managed via `ChatSystem.Data/ChatSystemDbContext.cs`. To apply migrations:

```bash
dotnet ef database update --project ChatSystem.Data --startup-project ChatSystem.Server
```

There are no test projects in this solution.

## Architecture Overview

Four-layer .NET 8 solution with separate web (ASP.NET Core) and desktop (WPF) presentation tiers:

```
Layer                  Project               Key Tech
─────                  ─────────             ─────────────────────────
Presentation (Web)     ChatSystem.Server     Razor MVC + SignalR Hub
Presentation (Desktop) ChatSystem.Wpf        WPF MVVM + SignalR Client
Data Access            ChatSystem.Data       EF Core + Repository Pattern
Core                   ChatSystem.Core       POCO Models, DTOs, Enums
Database               MySQL 5.7+           Via Pomelo.EntityFrameworkCore.MySql
```

### Project Dependencies (→ = references)

- `ChatSystem.Core` → *(no dependencies)*
- `ChatSystem.Data` → `ChatSystem.Core`
- `ChatSystem.Server` → `ChatSystem.Core` + `ChatSystem.Data`
- `ChatSystem.Wpf` → `ChatSystem.Core`

### Server (`ChatSystem.Server`)

ASP.NET Core web app hosting three concerns:

1. **REST API** — `Controllers/Api/` — JWT-authenticated JSON endpoints (auth, users, friends, groups, messages, admin). Uses `[Authorize]` with a custom `AdminOnly` policy.
2. **MVC Pages** — `Controllers/Mvc/` + `Views/` — server-rendered pages (login, register, chat, admin dashboard). Bootstrap 5 + jQuery frontend with vendored libs in `wwwroot/lib/`.
3. **SignalR Hub** — `Hubs/ChatHub.cs` — real-time WebSocket hub at `/hubs/chat` handling online tracking, private & group messaging, typing indicators.

The `Program.cs` configures services in order: DbContext → JWT auth → SignalR → MVC → Swagger → CORS.

### WPF Client (`ChatSystem.Wpf`)

MVVM desktop client communicating with the server over HTTP (via `ApiService`) and WebSocket (via `SignalRService`):

- **Views/** — XAML windows (`LoginWindow`, `MainWindow`)
- **ViewModels/** — `LoginViewModel`, `MainViewModel` (friend list, messages, search, friend requests, online status), `AdminViewModel`
- **Services/** — `ApiService` (HttpClient wrapper), `AuthService` (token persistence via file), `SignalRService` (HubConnection lifecycle)
- **Converters/** — XAML value converters

### Data Layer (`ChatSystem.Data`)

EF Core `DbContext` with full Fluent API configuration. Repository pattern with interfaces + implementations:

- `IUserRepository` / `UserRepository` — CRUD, search, pending user approval, pagination
- `IFriendRepository` / `FriendRepository` — bidirectional friendship, request/accept/reject
- `IMessageRepository` / `MessageRepository` — private messages, soft delete, admin force delete
- `IGroupRepository` / `GroupRepository` — group CRUD, member management, group messages

### Core Layer (`ChatSystem.Core`)

Pure data structures with no dependencies:

- **Models/** — Entity models: `User`, `Friend`, `FriendRequest`, `PrivateMessage`, `Group`, `GroupMember`, `GroupMessage`
- **DTOs/** — `ApiResponse<T>`, auth DTOs, friend DTOs, group DTOs, `MessageDTO`, `PagedResult<T>`, `UserDTO`
- **Enums/** — `UserRole` (User/Admin), `UserStatus` (Pending/Active/Banned), `MessageType` (Text/File/System), `FriendRequestStatus` (Pending/Accepted/Rejected)

### Key Patterns

- **JWT Authentication** — Token generated in `Helpers/JwtHelper.cs` with claims (userId, username, role). Configured via `Jwt:Key` (32+ chars), `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpireHours` in `appsettings.json`.
- **Password Hashing** — BCrypt via `BCrypt.Net-Next` 4.0.3
- **User Approval Flow** — Users register with `UserStatus.Pending`; admins must approve before they can log in.
- **Role-Based Admin** — Custom `AdminOnly` authorization policy requiring `UserRole.Admin`.

### Development Configuration

- File upload max size: 10 MB, stored in `wwwroot/uploads`
- CORS is configured in `Program.cs` for WPF client and development origins
- JWT token expiry: 24 hours (configurable)
