namespace WPFApp;

/// <summary>
/// Представляет одну запись, импортированную из CSV-файла.
/// </summary>
public class PersonRecord
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string SurName { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
}
