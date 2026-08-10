namespace WPFApp.Services;

/// <summary>
/// Содержит одну страницу результатов поиска и общее число найденных записей.
/// </summary>
internal class RecordSearchResult
{
    public List<PersonRecord> Records { get; set; } = new();
    public int TotalCount { get; set; }
}
