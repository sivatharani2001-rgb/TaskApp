using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["Jwt:Key"]!;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// Database: SQLite for zero-setup learning. See README to switch to SQL Server.
builder.Services.AddDbContext<AppDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// Authentication: the API trusts requests carrying a valid JWT in "Authorization: Bearer ..."
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// Create the database and tables on first run
using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDb>().Database.EnsureCreated();

app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

string MakeToken(User u)
{
    var token = new JwtSecurityToken(
        claims: new[] { new Claim("uid", u.Id.ToString()), new Claim("email", u.Email) },
        expires: DateTime.UtcNow.AddHours(2),
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
    return new JwtSecurityTokenHandler().WriteToken(token);
}

int CurrentUserId(ClaimsPrincipal p) => int.Parse(p.FindFirstValue("uid")!);

// ---------- Auth endpoints ----------
var auth = app.MapGroup("/api/auth");

auth.MapPost("/signup", async (AuthRequest req, AppDb db) =>
{
    var email = req.Email?.Trim().ToLowerInvariant() ?? "";
    if (!email.Contains('@')) return Results.BadRequest(new { error = "Enter a valid email address." });
    if ((req.Password?.Length ?? 0) < 6) return Results.BadRequest(new { error = "Password must be at least 6 characters." });
    if (await db.Users.AnyAsync(u => u.Email == email))
        return Results.Conflict(new { error = "An account with this email already exists." });

    var user = new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password) };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok(new { token = MakeToken(user), email = user.Email });
});

auth.MapPost("/login", async (AuthRequest req, AppDb db) =>
{
    var email = req.Email?.Trim().ToLowerInvariant() ?? "";
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password ?? "", user.PasswordHash))
        return Results.Json(new { error = "Email or password is incorrect." }, statusCode: 401);
    return Results.Ok(new { token = MakeToken(user), email = user.Email });
});

// ---------- Task CRUD (login required; each user only sees their own tasks) ----------
var tasks = app.MapGroup("/api/tasks").RequireAuthorization();

// READ all
tasks.MapGet("/", async (ClaimsPrincipal me, AppDb db) =>
{
    var uid = CurrentUserId(me);
    return await db.Tasks.Where(t => t.UserId == uid)
                         .OrderByDescending(t => t.CreatedAt).ToListAsync();
});

// READ one
tasks.MapGet("/{id:int}", async (int id, ClaimsPrincipal me, AppDb db) =>
{
    var uid = CurrentUserId(me);
    var t = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
    return t is null ? Results.NotFound() : Results.Ok(t);
});

// CREATE
tasks.MapPost("/", async (TaskRequest req, ClaimsPrincipal me, AppDb db) =>
{
    if (string.IsNullOrWhiteSpace(req.Title)) return Results.BadRequest(new { error = "Title is required." });
    var t = new TaskItem { Title = req.Title.Trim(), Notes = req.Notes, IsDone = req.IsDone, UserId = CurrentUserId(me) };
    db.Tasks.Add(t);
    await db.SaveChangesAsync();
    return Results.Created($"/api/tasks/{t.Id}", t);
});

// UPDATE
tasks.MapPut("/{id:int}", async (int id, TaskRequest req, ClaimsPrincipal me, AppDb db) =>
{
    var uid = CurrentUserId(me);
    var t = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
    if (t is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(req.Title)) return Results.BadRequest(new { error = "Title is required." });
    t.Title = req.Title.Trim(); t.Notes = req.Notes; t.IsDone = req.IsDone;
    await db.SaveChangesAsync();
    return Results.Ok(t);
});

// DELETE
tasks.MapDelete("/{id:int}", async (int id, ClaimsPrincipal me, AppDb db) =>
{
    var uid = CurrentUserId(me);
    var t = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
    if (t is null) return Results.NotFound();
    db.Tasks.Remove(t);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
