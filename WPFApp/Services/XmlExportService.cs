using System.IO;
using System.Xml;

namespace WPFApp.Services;

/// <summary>
/// Сохраняет выбранные записи в XML-файл требуемого формата.
/// </summary>
internal class XmlExportService
{
    public async Task ExportAsync(
        string filePath,
        IAsyncEnumerable<PersonRecord> records)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Не указан путь для сохранения XML-файла.", nameof(filePath));
        }
        ArgumentNullException.ThrowIfNull(records);

        var settings = new XmlWriterSettings
        {
            Async = true,
            Indent = true,
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };

        await using var stream = File.Create(filePath);
        await using var writer = XmlWriter.Create(stream, settings);

        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "TestProgram", null);

        await foreach (var record in records)
        {
            await WriteRecordAsync(writer, record);
        }

        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
    }

    private static async Task WriteRecordAsync(XmlWriter writer, PersonRecord record)
    {
        await writer.WriteStartElementAsync(null, "Record", null);
        await writer.WriteAttributeStringAsync(null, "id", null, record.Id.ToString());
        await writer.WriteElementStringAsync(null, "Date", null, record.Date.ToString("dd.MM.yyyy"));
        await writer.WriteElementStringAsync(null, "FirstName", null, record.FirstName);
        await writer.WriteElementStringAsync(null, "LastName", null, record.LastName);
        await writer.WriteElementStringAsync(null, "SurName", null, record.SurName);
        await writer.WriteElementStringAsync(null, "City", null, record.City);
        await writer.WriteElementStringAsync(null, "Country", null, record.Country);
        await writer.WriteEndElementAsync();
    }
}
