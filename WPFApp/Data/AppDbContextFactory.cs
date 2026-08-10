using Microsoft.EntityFrameworkCore;

namespace WPFApp.Data;

/// <summary>
/// Создаёт контекст базы данных с настройками приложения.
/// </summary>
internal class AppDbContextFactory
{
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContextFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Строка подключения к базе данных не задана.", nameof(connectionString));
        }

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
    }

    public AppDbContext CreateContext()
    {
        return new AppDbContext(_options);
    }
}
