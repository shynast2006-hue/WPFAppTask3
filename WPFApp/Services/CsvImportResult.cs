namespace WPFApp.Services;

/// <summary>
/// Содержит результат импорта CSV-файла.
/// </summary>
internal class CsvImportResult
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
}
