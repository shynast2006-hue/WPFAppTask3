using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace WPFApp.Services;

/// <summary>
/// Сохраняет записи в Excel-файл, разбивая большой результат на несколько листов.
/// </summary>
internal class ExcelExportService
{
    private const int MaximumRowsPerSheet = 1_048_576;
    private const int MaximumRecordsPerSheet = MaximumRowsPerSheet - 1;

    public async Task ExportAsync(
        string filePath,
        IAsyncEnumerable<PersonRecord> records)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Не указан путь для сохранения Excel-файла.", nameof(filePath));
        }

        ArgumentNullException.ThrowIfNull(records);

        using var document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var sheets = workbookPart.Workbook.AppendChild(new Sheets());

        uint sheetId = 1;
        uint rowNumber = 1;
        uint recordsOnSheet = 0;
        var writer = CreateWorksheet(workbookPart, sheets, sheetId, rowNumber);

        try
        {
            await foreach (var record in records)
            {
                if (recordsOnSheet == MaximumRecordsPerSheet)
                {
                    CloseWorksheet(writer);
                    sheetId++;
                    rowNumber = 1;
                    recordsOnSheet = 0;
                    writer = CreateWorksheet(workbookPart, sheets, sheetId, rowNumber);
                }

                rowNumber++;
                recordsOnSheet++;
                WriteRecord(writer, record, rowNumber);
            }
        }
        finally
        {
            CloseWorksheet(writer);
        }

        workbookPart.Workbook.Save();
    }

    private static OpenXmlWriter CreateWorksheet(
        WorkbookPart workbookPart,
        Sheets sheets,
        uint sheetId,
        uint rowNumber)
    {
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        var relationshipId = workbookPart.GetIdOfPart(worksheetPart);
        sheets.Append(new Sheet
        {
            Id = relationshipId,
            SheetId = sheetId,
            Name = $"Записи {sheetId}"
        });

        var writer = OpenXmlWriter.Create(worksheetPart);
        writer.WriteStartElement(new Worksheet());
        writer.WriteStartElement(new SheetData());
        WriteRow(writer, rowNumber, new[] { "Дата", "Имя", "Фамилия", "Отчество", "Город", "Страна" });
        return writer;
    }

    private static void WriteRecord(OpenXmlWriter writer, PersonRecord record, uint rowNumber)
    {
        WriteRow(writer, rowNumber, new[]
        {
            record.Date.ToString("dd.MM.yyyy"),
            record.FirstName,
            record.LastName,
            record.SurName,
            record.City,
            record.Country
        });
    }

    private static void WriteRow(OpenXmlWriter writer, uint rowNumber, IEnumerable<string> values)
    {
        writer.WriteStartElement(new Row { RowIndex = rowNumber });

        foreach (var value in values)
        {
            writer.WriteElement(new Cell
            {
                DataType = CellValues.InlineString,
                InlineString = new InlineString(new Text(value))
            });
        }

        writer.WriteEndElement();
    }

    private static void CloseWorksheet(OpenXmlWriter writer)
    {
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.Dispose();
    }
}
