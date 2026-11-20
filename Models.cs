using Microsoft.EntityFrameworkCore;

// ---- Database entities ----
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public List<TaskItem> Tasks { get; set; } = new();
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public bool IsDone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }          // owner: every task belongs to one user
    public User? User { get; set; }
}

// ---- Database context ----
public class AppDb : DbContext
{
    public AppDb(DbContextOptions<AppDb> options) : base(options) { }
    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder b)
        => b.Entity<User>().HasIndex(u => u.Email).IsUnique();
}

// ---- Request shapes (DTOs) ----
public record AuthRequest(string Email, string Password);
public record TaskRequest(string Title, string? Notes, bool IsDone);
