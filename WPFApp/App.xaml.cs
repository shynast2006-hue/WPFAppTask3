using System.Diagnostics;
using System.Windows;
using WPFApp.Data;

namespace WPFApp;

/// <summary>
/// Управляет запуском приложения и инициализацией базы данных.
/// </summary>
public partial class App : Application
{
    public const string ConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=WpfStarterRecords;Trusted_Connection=True;TrustServerCertificate=True;";

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var contextFactory = new AppDbContextFactory(ConnectionString);
            using var context = contextFactory.CreateContext();
            context.Database.EnsureCreated();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            MessageBox.Show(
                "Не удалось подготовить базу данных. Проверьте, что MS SQL Server LocalDB установлен, и перезапустите приложение.",
                "Ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }
}
