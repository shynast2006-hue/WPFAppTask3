using System.Globalization;
using System.IO;
using Microsoft.EntityFrameworkCore;
using WPFApp.Data;

namespace WPFApp.Services;

/// <summary>
/// Заменяет записи в базе данных данными из CSV-файла.
/// </summary>
internal class CsvImportService
{
    private const int BatchSize = 1000;
    private static readonly string[] DateFormats = { "dd.MM.yyyy", "d.MM.yyyy", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-dd" };

    private readonly AppDbContextFactory _contextFactory;

    public CsvImportService(string connectionString)
    {
        _contextFactory = new AppDbContextFactory(connectionString);
    }

    public async Task<CsvImportResult> ImportAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Не указан путь к CSV-файлу.", nameof(filePath));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("CSV-файл не найден.", filePath);
        }

        if (!string.Equals(Path.GetExtension(filePath), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Выбранный файл имеет неверный формат. Выберите файл с расширением .csv.");
        }

        var importedCount = 0;
        var skippedCount = 0;
        var batch = new List<PersonRecord>(BatchSize);

        await using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);
        using var context = _contextFactory.CreateContext();

        string? line;
        PersonRecord? firstRecord = null;

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                skippedCount++;
                continue;
            }

            if (TryParseRecord(line, out var record))
            {
                firstRecord = record;
                importedCount++;
                break;
            }

            skippedCount++;
        }

        if (firstRecord is null)
        {
            throw new InvalidDataException(
                "Файл не содержит ни одной корректной CSV-записи. Проверьте формат данных.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.PersonRecords.ExecuteDeleteAsync();
        batch.Add(firstRecord);

        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                skippedCount++;
                continue;
            }

            if (TryParseRecord(line, out var record))
            {
                batch.Add(record);
                importedCount++;
            }
            else
            {
                skippedCount++;
            }

            if (batch.Count >= BatchSize)
            {
                await SaveBatchAsync(context, batch);
            }
        }

        if (batch.Count > 0)
        {
            await SaveBatchAsync(context, batch);
        }

        await transaction.CommitAsync();

        return new CsvImportResult
        {
            ImportedCount = importedCount,
            SkippedCount = skippedCount
        };
    }

    private static async Task SaveBatchAsync(
        AppDbContext context,
        List<PersonRecord> batch)
    {
        await context.PersonRecords.AddRangeAsync(batch);
        await context.SaveChangesAsync();

        batch.Clear();
        context.ChangeTracker.Clear();
    }

    private static bool TryParseRecord(string line, out PersonRecord record)
    {
        record = null!;

        var parts = line.Split(';');
        if (parts.Length != 6)
        {
            return false;
        }

        if (!TryParseDate(parts[0], out var date))
        {
            return false;
        }

        var firstName = parts[1].Trim();
        var lastName = parts[2].Trim();
        var surName = parts[3].Trim();
        var city = parts[4].Trim();
        var country = parts[5].Trim();

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(surName) ||
            string.IsNullOrWhiteSpace(city) ||
            string.IsNullOrWhiteSpace(country))
        {
            return false;
        }

        record = new PersonRecord
        {
            Date = date,
            FirstName = firstName,
            LastName = lastName,
            SurName = surName,
            City = city,
            Country = country
        };

        return true;
    }

    private static bool TryParseDate(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(
            value.Trim(),
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }
}
