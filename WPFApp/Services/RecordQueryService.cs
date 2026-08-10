using Microsoft.EntityFrameworkCore;
using WPFApp.Data;

namespace WPFApp.Services;

/// <summary>
/// Выполняет LINQ-запросы для поиска записей в базе данных.
/// </summary>
internal class RecordQueryService
{
    private readonly AppDbContextFactory _contextFactory;

    public RecordQueryService(string connectionString)
    {
        _contextFactory = new AppDbContextFactory(connectionString);
    }

    public async Task<RecordSearchResult> GetPageAsync(
        RecordFilter filter,
        int pageNumber,
        int pageSize)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filter.Validate();

        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "Номер страницы должен быть больше нуля.");
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Размер страницы должен быть больше нуля.");
        }

        await using var context = _contextFactory.CreateContext();
        var query = ApplyFilter(context.PersonRecords.AsNoTracking(), filter);
        var totalCount = await query.CountAsync();
        var records = await OrderRecords(query)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new RecordSearchResult
        {
            Records = records,
            TotalCount = totalCount
        };
    }

    public async IAsyncEnumerable<PersonRecord> GetRecordsForExportAsync(RecordFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filter.Validate();

        await using var context = _contextFactory.CreateContext();
        var query = OrderRecords(ApplyFilter(context.PersonRecords.AsNoTracking(), filter))
            .AsAsyncEnumerable();

        await foreach (var record in query)
        {
            yield return record;
        }
    }

    private static IQueryable<PersonRecord> ApplyFilter(
        IQueryable<PersonRecord> query,
        RecordFilter filter)
    {
        if (filter.FromDate.HasValue)
        {
            query = query.Where(record => record.Date >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(record => record.Date <= filter.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.FirstName))
        {
            var value = filter.FirstName.Trim();
            query = query.Where(record => record.FirstName.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(filter.LastName))
        {
            var value = filter.LastName.Trim();
            query = query.Where(record => record.LastName.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(filter.SurName))
        {
            var value = filter.SurName.Trim();
            query = query.Where(record => record.SurName.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            var value = filter.City.Trim();
            query = query.Where(record => record.City.Contains(value));
        }

        if (!string.IsNullOrWhiteSpace(filter.Country))
        {
            var value = filter.Country.Trim();
            query = query.Where(record => record.Country.Contains(value));
        }

        return query;
    }

    private static IOrderedQueryable<PersonRecord> OrderRecords(IQueryable<PersonRecord> query)
    {
        return query
            .OrderBy(record => record.Date)
            .ThenBy(record => record.LastName)
            .ThenBy(record => record.FirstName);
    }
}
