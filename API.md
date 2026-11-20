# Task Notebook API reference

**Base URL:** the address printed by `dotnet run` (for example `http://localhost:5000`). All routes start with `/api`.
**Format:** JSON requests and responses, with camelCase property names.

## Authentication

Signup and login return a JWT. Send it on every `/api/tasks` request:

```
Authorization: Bearer <token>
```

Tokens expire after 2 hours, after which the API returns `401` and you log in again. If the token is missing or invalid, the response is a `401` with an empty body, not JSON.

## Error format

Errors raised by the endpoints themselves look like this:

```json
{ "error": "Human-readable message" }
```

## Auth endpoints

### POST `/api/auth/signup`

Creates an account and logs you in. The email is trimmed and lowercased before it is stored.

**Request body**

```json
{ "email": "ada@example.com", "password": "secret123" }
```

| Status | Meaning | Body |
|---|---|---|
| 200 | Account created | `{ "token": "eyJ...", "email": "ada@example.com" }` |
| 400 | Email has no `@`, or password is under 6 characters | `{ "error": "..." }` |
| 409 | Email already registered | `{ "error": "An account with this email already exists." }` |

### POST `/api/auth/login`

**Request body:** same shape as signup.

| Status | Meaning | Body |
|---|---|---|
| 200 | Success | `{ "token": "eyJ...", "email": "ada@example.com" }` |
| 401 | Wrong email or password | `{ "error": "Email or password is incorrect." }` |

## Task endpoints

All of these require the Bearer token. Each user only sees and changes their own tasks.

**Task object**

```json
{
  "id": 1,
  "title": "Buy milk",
  "notes": "Oat, not dairy",
  "isDone": false,
  "createdAt": "2026-11-18T04:30:00Z",
  "userId": 3,
  "user": null
}
```

`user` is always `null` and can be ignored.

### GET `/api/tasks`

Lists your tasks, newest first.

- **200:** an array of task objects, empty if you have none.

### GET `/api/tasks/{id}`

- **200:** a task object.
- **404:** the task doesn't exist, or belongs to someone else.

### POST `/api/tasks`

Creates a task.

**Request body**

```json
{ "title": "Buy milk", "notes": "Oat, not dairy", "isDone": false }
```

`title` is required. `notes` is optional and `isDone` defaults to `false`.

- **201:** the created task, with a `Location: /api/tasks/{id}` header.
- **400:** `{ "error": "Title is required." }`

### PUT `/api/tasks/{id}`

Replaces a task's editable fields. It is a full replace, not a partial update: any field you leave out is reset (`notes` becomes `null`, `isDone` becomes `false`). To tick a task done, resend the title and notes along with `"isDone": true`. The frontend does this.

**Request body:** same shape as POST.

- **200:** the updated task.
- **400:** title is empty.
- **404:** not found, or not yours.

### DELETE `/api/tasks/{id}`

- **204:** deleted, with no body.
- **404:** not found, or not yours.

## Status code summary

| Code | Used for |
|---|---|
| 200 / 201 / 204 | Success, created, deleted |
| 400 | Validation failed |
| 401 | Bad login, or missing/expired token |
| 404 | Task not found (also returned for other users' tasks) |
| 409 | Duplicate email on signup |

## Trying it from PowerShell

In PowerShell, `curl` is an alias for something else, so use `Invoke-RestMethod`:

```powershell
$base = "http://localhost:5000"   # use the port dotnet run printed

# Sign up and keep the token
$r = Invoke-RestMethod "$base/api/auth/signup" -Method Post -ContentType "application/json" `
     -Body '{"email":"ada@example.com","password":"secret123"}'
$h = @{ Authorization = "Bearer $($r.token)" }

# Create
Invoke-RestMethod "$base/api/tasks" -Method Post -Headers $h -ContentType "application/json" `
     -Body '{"title":"Buy milk","notes":"Oat","isDone":false}'

# List
Invoke-RestMethod "$base/api/tasks" -Headers $h

# Update task 1 (full replace)
Invoke-RestMethod "$base/api/tasks/1" -Method Put -Headers $h -ContentType "application/json" `
     -Body '{"title":"Buy milk","notes":"Oat","isDone":true}'

# Delete
Invoke-RestMethod "$base/api/tasks/1" -Method Delete -Headers $h
```
