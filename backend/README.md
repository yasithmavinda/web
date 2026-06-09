# TaskFlow Backend — ASP.NET Core 8 Web API

Complete Task Management System backend built with Clean Architecture.

---

## 🗂️ Project Structure

```
backend/
├── TaskFlow.sln
└── src/
    ├── TaskFlow.Domain/              ← Entities, Enums, Exceptions (no dependencies)
    │   ├── Entities/
    │   │   ├── User.cs
    │   │   ├── Role.cs
    │   │   ├── AuthEntities.cs       ← RefreshToken, PasswordResetToken, AuditLog
    │   │   ├── Project.cs
    │   │   ├── Task.cs               ← TaskItem, TaskAssignment, TaskStatusHistory, Tag
    │   │   └── Communication.cs      ← Comment, Attachment, Notification, NotificationSetting
    │   ├── Enums/
    │   │   └── Enums.cs              ← TaskStatus, TaskPriority, ProjectStatus, NotificationType
    │   └── Exceptions/
    │       └── DomainExceptions.cs   ← NotFoundException, UnauthorizedException, etc.
    │
    ├── TaskFlow.Application/         ← Business Logic (depends on Domain only)
    │   ├── Common/
    │   │   ├── Interfaces/
    │   │   │   ├── IRepositories.cs  ← All repository contracts
    │   │   │   └── IServices.cs      ← IJwtService, IPasswordHasher, ICurrentUserService, IEmailService
    │   │   ├── Models/
    │   │   │   └── ApiResponse.cs    ← ApiResponse<T>, PagedResult<T>
    │   │   └── Mappings/
    │   │       └── MappingProfile.cs ← AutoMapper entity → DTO mappings
    │   ├── DTOs/
    │   │   ├── Auth/
    │   │   │   ├── AuthDtos.cs       ← RegisterDto, LoginDto, AuthResultDto, SessionDto
    │   │   │   └── UserDtos.cs       ← UserDto, UserSummaryDto, UserStatsDto, WorkloadDto
    │   │   ├── Tasks/
    │   │   │   └── TaskDtos.cs       ← TaskDto, CreateTaskDto, UpdateTaskDto, TaskFilterDto
    │   │   ├── Projects/
    │   │   │   └── ProjectDtos.cs    ← ProjectDto, CreateProjectDto, ProjectMemberDto
    │   │   ├── Comments/
    │   │   │   └── CommentDtos.cs    ← CommentDto, CreateCommentDto
    │   │   └── Notifications/
    │   │       └── NotificationDtos.cs
    │   ├── Services/
    │   │   ├── AuthService.cs        ← Register, Login, Refresh, Logout, Password management
    │   │   ├── UserService.cs        ← CRUD, role assignment, stats, workload
    │   │   ├── TaskService.cs        ← Full task lifecycle + status history
    │   │   ├── ProjectService.cs     ← CRUD + member management
    │   │   └── CommentNotificationService.cs
    │   ├── Validators/
    │   │   └── Validators.cs         ← FluentValidation rules for all DTOs
    │   └── ApplicationServiceRegistration.cs
    │
    ├── TaskFlow.Infrastructure/      ← External concerns (DB, JWT, Email)
    │   ├── Persistence/
    │   │   ├── TaskFlowDbContext.cs  ← Full EF Core configuration + seed data
    │   │   └── Repositories/
    │   │       └── Repositories.cs   ← All 8 repository implementations
    │   ├── Security/
    │   │   ├── JwtService.cs         ← Token generation/validation + settings models
    │   │   └── CurrentUserService.cs ← Reads claims from HttpContext
    │   ├── Services/
    │   │   └── EmailService.cs       ← MailKit SMTP with HTML templates
    │   └── InfrastructureServiceRegistration.cs
    │
    └── TaskFlow.API/                 ← HTTP layer (Controllers, Middleware, SignalR)
        ├── Controllers/
        │   ├── BaseApiController.cs  ← CurrentUser helpers + response factories
        │   ├── AuthController.cs     ← 10 auth endpoints
        │   ├── UsersController.cs    ← 8 user management endpoints
        │   ├── ProjectsController.cs ← 8 project endpoints
        │   ├── TasksController.cs    ← 9 task endpoints
        │   └── CommentNotificationControllers.cs
        ├── Middleware/
        │   └── Middleware.cs         ← ExceptionMiddleware + RequestLoggingMiddleware
        ├── Hubs/
        │   └── NotificationHub.cs    ← SignalR real-time hub
        ├── Program.cs                ← Full DI, pipeline, middleware wiring
        ├── appsettings.json
        └── appsettings.Development.json
```

---

## 🚀 Setup & Run

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB, Express, or Developer Edition)
- Visual Studio 2022 or VS Code

### Step 1 — Configure Database Connection
Edit `src/TaskFlow.API/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TaskFlowDB;Trusted_Connection=true;"
  }
}
```

### Step 2 — Set JWT Secrets (Development)
```json
{
  "Jwt": {
    "AccessSecret": "any-random-string-at-least-32-characters-long",
    "RefreshSecret": "a-different-random-string-at-least-32-characters"
  }
}
```

### Step 3 — Run EF Core Migrations
```bash
cd backend

# Install EF Core tools (once)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API

# Apply to database (auto-run on startup too)
dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.API
```

### Step 4 — Run the API
```bash
cd backend/src/TaskFlow.API
dotnet run
```

### Step 5 — Open Swagger UI
Navigate to: **http://localhost:5000** (or https://localhost:5001)

---

## 🔐 Testing Authentication

### Register a user
```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "Test@1234",
  "roleId": 2
}
```

### Login
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "Test@1234"
}
```

Copy the `accessToken` from the response.

### Use protected endpoint
```http
GET /api/v1/auth/me
Authorization: Bearer <paste-access-token-here>
```

---

## ⚡ SignalR — Real-Time Connection

```javascript
// Frontend JavaScript example
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:5000/hubs/notifications', {
    accessTokenFactory: () => localStorage.getItem('accessToken')
  })
  .withAutomaticReconnect()
  .build();

// Subscribe to a project's updates
await connection.start();
await connection.invoke('JoinProject', projectId);

// Listen for events
connection.on('TaskCreated',       task   => console.log('New task:', task));
connection.on('TaskStatusChanged', task   => console.log('Status changed:', task));
connection.on('CommentAdded',      comment => console.log('New comment:', comment));
connection.on('NewNotification',   notif  => console.log('Notification:', notif));
```

---

## 📋 API Quick Reference

| Method | Endpoint | Auth | Role |
|--------|----------|------|------|
| POST | /api/v1/auth/register | ❌ | — |
| POST | /api/v1/auth/login | ❌ | — |
| POST | /api/v1/auth/refresh-token | ❌ | — |
| POST | /api/v1/auth/logout | ✅ | Any |
| POST | /api/v1/auth/forgot-password | ❌ | — |
| POST | /api/v1/auth/reset-password | ❌ | — |
| PUT | /api/v1/auth/change-password | ✅ | Any |
| GET | /api/v1/users | ✅ | Admin |
| PUT | /api/v1/users/profile | ✅ | Any |
| PATCH | /api/v1/users/{id}/role | ✅ | Admin |
| GET | /api/v1/projects | ✅ | Any |
| POST | /api/v1/projects | ✅ | PM/Admin |
| POST | /api/v1/projects/{id}/members | ✅ | PM/Admin |
| GET | /api/v1/tasks | ✅ | Any |
| POST | /api/v1/tasks | ✅ | PM/Admin |
| PATCH | /api/v1/tasks/{id}/status | ✅ | Any |
| PATCH | /api/v1/tasks/{id}/position | ✅ | Any |
| PUT | /api/v1/tasks/{id}/assignees | ✅ | PM/Admin |
| POST | /api/v1/comments | ✅ | Any |
| GET | /api/v1/notifications | ✅ | Any |
| GET | /api/v1/notifications/unread-count | ✅ | Any |
| POST | /api/v1/notifications/mark-all-read | ✅ | Any |

---

## 🔒 Security Checklist
- ✅ BCrypt password hashing (work factor 12)
- ✅ JWT access tokens (15 min expiry)
- ✅ Refresh token rotation (reuse detection)
- ✅ Token hashes stored in DB (not raw tokens)
- ✅ Account lockout (5 attempts → 15 min)
- ✅ Rate limiting (5/min login, 3/hr password reset)
- ✅ Security response headers
- ✅ CORS restriction
- ✅ Soft delete (data preserved)
- ✅ Full audit logging

---

## 🗺️ Next Steps
- [ ] Add EF Core Migration and seed data
- [ ] Configure production SQL Server
- [ ] Set environment variables for secrets
- [ ] Deploy to Azure App Service / Docker
- [ ] Build React Frontend
