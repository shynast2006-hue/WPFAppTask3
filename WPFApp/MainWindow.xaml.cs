using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using WPFApp.Services;

namespace WPFApp;

/// <summary>
/// Содержит интерфейс импорта, поиска и экспорта записей.
/// </summary>
public partial class MainWindow : Window
{
    private const int PageSize = 100;

    private readonly CsvImportService _csvImportService = new(App.ConnectionString);
    private readonly RecordQueryService _recordQueryService = new(App.ConnectionString);
    private readonly ExcelExportService _excelExportService = new();
    private readonly XmlExportService _xmlExportService = new();

    private List<PersonRecord> _records = new(PageSize);
    private RecordFilter _currentFilter = new();
    private int _currentPage = 1;
    private int _totalRecords;
    private bool _hasOpenedFile;
    private string? _selectedCsvPath;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _records = new List<PersonRecord>(PageSize);
        RecordsGrid.ItemsSource = _records;
        StatusTextBlock.Text = "Выберите CSV-файл.";
        UpdatePageControls();
    }

    private void ChooseCsvButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "CSV-файлы (*.csv)|*.csv",
            Title = "Выберите CSV-файл"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _selectedCsvPath = dialog.FileName;
        CsvPathTextBox.Text = _selectedCsvPath;
        StatusTextBlock.Text = "CSV-файл выбран.";
    }

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedCsvPath))
        {
            MessageBox.Show("Сначала выберите CSV-файл.", "Импорт", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteOperationAsync(
            "Импорт данных...",
            "Не удалось импортировать CSV-файл.",
            async () =>
            {
                var result = await _csvImportService.ImportAsync(_selectedCsvPath);
                _hasOpenedFile = true;
                await LoadRecordsAsync(resetPage: true);

                MessageBox.Show(
                    $"Данные заменены.\n\nДобавлено записей: {result.ImportedCount}\nПропущено строк: {result.SkippedCount}",
                    "Импорт",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            });
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasOpenedFile())
        {
            return;
        }

        await ExecuteOperationAsync(
            "Поиск записей...",
            "Не удалось выполнить поиск.",
            () => LoadRecordsAsync(resetPage: true));
    }

    private async void PreviousPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (HasOpenedFile() && _currentPage > 1)
        {
            _currentPage--;
            await ExecuteOperationAsync(
                "Поиск записей...",
                "Не удалось выполнить поиск.",
                () => LoadRecordsAsync());
        }
    }

    private async void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (HasOpenedFile() && _currentPage < GetPageCount())
        {
            _currentPage++;
            await ExecuteOperationAsync(
                "Поиск записей...",
                "Не удалось выполнить поиск.",
                () => LoadRecordsAsync());
        }
    }

    private async void ExportExcelButton_Click(object sender, RoutedEventArgs e)
    {
        await ExportFileAsync(
            "Excel-файл (*.xlsx)|*.xlsx",
            "Экспорт в Excel",
            "xlsx",
            "Создание Excel-файла...",
            "Excel-файл успешно создан.",
            "Не удалось создать Excel-файл.",
            ExportExcelAsync);
    }

    private async void ExportXmlButton_Click(object sender, RoutedEventArgs e)
    {
        await ExportFileAsync(
            "XML-файл (*.xml)|*.xml",
            "Экспорт в XML",
            "xml",
            "Создание XML-файла...",
            "XML-файл успешно создан.",
            "Не удалось создать XML-файл.",
            ExportXmlAsync);
    }

    private async Task LoadRecordsAsync(bool resetPage = false)
    {
        if (resetPage)
        {
            _currentPage = 1;
            _currentFilter = CreateFilter();
        }

        var result = await _recordQueryService.GetPageAsync(_currentFilter, _currentPage, PageSize);
        _records = result.Records;
        _totalRecords = result.TotalCount;
        RecordsGrid.ItemsSource = _records;
        StatusTextBlock.Text = _totalRecords == 0
            ? "Записи не найдены."
            : $"Найдено записей: {_totalRecords}.";
        UpdatePageControls();
    }

    private async Task ExportFileAsync(
        string filter,
        string title,
        string extension,
        string processingStatus,
        string successStatus,
        string errorMessage,
        Func<string, Task> exportAsync)
    {
        if (!HasRecordsForExport() ||
            !TryGetExportPath(filter, title, extension, out var filePath))
        {
            return;
        }

        await ExecuteOperationAsync(
            processingStatus,
            errorMessage,
            async () =>
            {
                await exportAsync(filePath);
                StatusTextBlock.Text = successStatus;
            });
    }

    private async Task ExportExcelAsync(string filePath)
    {
        await _excelExportService.ExportAsync(
            filePath,
            _recordQueryService.GetRecordsForExportAsync(_currentFilter));
    }

    private Task ExportXmlAsync(string filePath)
    {
        return _xmlExportService.ExportAsync(
            filePath,
            _recordQueryService.GetRecordsForExportAsync(_currentFilter));
    }

    private async Task ExecuteOperationAsync(
        string processingStatus,
        string errorMessage,
        Func<Task> operation)
    {
        try
        {
            SetBusyState(true, processingStatus);
            await operation();
        }
        catch (Exception exception)
        {
            ShowError(errorMessage, exception);
        }
        finally
        {
            SetBusyState(false, StatusTextBlock.Text);
        }
    }

    private RecordFilter CreateFilter()
    {
        return new RecordFilter
        {
            FromDate = ToDateOnly(FromDatePicker.SelectedDate),
            ToDate = ToDateOnly(ToDatePicker.SelectedDate),
            FirstName = FirstNameTextBox.Text,
            LastName = LastNameTextBox.Text,
            SurName = SurNameTextBox.Text,
            City = CityTextBox.Text,
            Country = CountryTextBox.Text
        };
    }

    private static DateOnly? ToDateOnly(DateTime? date)
    {
        return date.HasValue ? DateOnly.FromDateTime(date.Value) : null;
    }

    private static bool TryGetExportPath(string filter, string title, string extension, out string filePath)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            Title = title,
            DefaultExt = extension,
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
        {
            filePath = string.Empty;
            return false;
        }

        filePath = dialog.FileName;
        return true;
    }

    private bool HasRecordsForExport()
    {
        if (_totalRecords > 0)
        {
            return true;
        }

        MessageBox.Show(
            "Сначала выполните поиск, который найдёт хотя бы одну запись.",
            "Экспорт",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private bool HasOpenedFile()
    {
        if (_hasOpenedFile)
        {
            return true;
        }

        MessageBox.Show(
            "Сначала выберите CSV-файл и нажмите «Открыть».",
            "Поиск",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        return false;
    }

    private void SetBusyState(bool isBusy, string status)
    {
        MainContentGrid.IsEnabled = !isBusy;
        Cursor = isBusy ? Cursors.Wait : Cursors.Arrow;
        StatusTextBlock.Text = status;
    }

    private void ShowError(string message, Exception exception)
    {
        Debug.WriteLine(exception);
        StatusTextBlock.Text = message;
        MessageBox.Show(
            $"{message}\n\n{GetErrorDetails(exception)}",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static string GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            InvalidDataException => exception.Message,
            UnauthorizedAccessException => "Нет доступа к выбранному файлу или папке.",
            IOException => "Не удалось прочитать или сохранить файл. Возможно, он открыт в другой программе.",
            ArgumentException or InvalidOperationException => exception.Message,
            _ => "Проверьте выбранные данные и повторите попытку."
        };
    }

    private int GetPageCount()
    {
        return (int)Math.Ceiling((double)_totalRecords / PageSize);
    }

    private void UpdatePageControls()
    {
        var pageCount = GetPageCount();
        PageTextBlock.Text = pageCount == 0 ? "Страниц нет" : $"Страница {_currentPage} из {pageCount}";
        PreviousPageButton.IsEnabled = _currentPage > 1;
        NextPageButton.IsEnabled = _currentPage < pageCount;
    }
}
