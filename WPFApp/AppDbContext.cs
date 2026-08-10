using Microsoft.EntityFrameworkCore;
using WPFApp;

namespace WPFApp.Data;

/// <summary>
/// Представляет подключение приложения к базе данных SQL Server.
/// </summary>
internal class AppDbContext : DbContext
{
    public DbSet<PersonRecord> PersonRecords { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}
