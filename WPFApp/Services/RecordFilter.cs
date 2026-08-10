namespace WPFApp.Services;

/// <summary>
/// Содержит необязательные условия поиска записей.
/// </summary>
internal class RecordFilter
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? SurName { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }

    public void Validate()
    {
        if (FromDate.HasValue && ToDate.HasValue && FromDate > ToDate)
        {
            throw new ArgumentException("Дата начала не может быть позже даты окончания.");
        }
    }
}
