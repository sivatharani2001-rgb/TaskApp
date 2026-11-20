# Task Notebook – learning project

A small single-page app: .NET 8 minimal API + SQLite (via EF Core) + plain HTML/CSS/JS.

## Run it
1. Install the .NET 8 SDK: https://dotnet.microsoft.com/download
2. In this folder run:  `dotnet run`
3. Open the URL printed in the console (e.g. http://localhost:5000), sign up, and add tasks.

## How it fits together
| Piece | File | What it teaches |
|---|---|---|
| Entities + DbContext | `Models.cs` | EF Core code-first, one-to-many (User -> Tasks) |
| REST API | `Program.cs` | Minimal API, route groups, status codes |
| Auth | `Program.cs` | BCrypt password hashing, JWT issue + validate |
| SPA | `wwwroot/*` | fetch + Bearer token, view switching, DOM rendering |

## API
| Method | Route | Auth | Purpose |
|---|---|---|---|
| POST | /api/auth/signup | no | create account, returns JWT |
| POST | /api/auth/login | no | returns JWT |
| GET | /api/tasks | yes | list my tasks |
| GET | /api/tasks/{id} | yes | one task |
| POST | /api/tasks | yes | create |
| PUT | /api/tasks/{id} | yes | update |
| DELETE | /api/tasks/{id} | yes | delete |

## Switch to SQL Server
1. `dotnet remove package Microsoft.EntityFrameworkCore.Sqlite`
2. `dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.*`
3. In `Program.cs` change `UseSqlite(` to `UseSqlServer(`
4. In `appsettings.json` set:
   `"Default": "Server=(localdb)\\MSSQLLocalDB;Database=TaskApp;Trusted_Connection=True;"`
(Delete `taskapp.db` if it exists. Later, replace `EnsureCreated()` with EF migrations.)

## Next steps to practice
- Add a `DueDate` field end-to-end (model, API, UI)
- Replace `EnsureCreated()` with `dotnet ef migrations`
- Move the JWT key to user-secrets: `dotnet user-secrets set "Jwt:Key" "..."`
- Add a refresh-token flow, or move the token to an HttpOnly cookie

## Learning-project caveats
Use HTTPS and a real secret in production, add rate limiting on login, and don't rely on localStorage for tokens in sensitive apps.
