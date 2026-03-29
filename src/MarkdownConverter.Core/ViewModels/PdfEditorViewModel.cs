using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MarkdownConverter.Services;
using MarkdownConverter.Platform;

namespace MarkdownConverter.ViewModels
{
    public class PdfEditorViewModel : ViewModelBase
    {
        private readonly PdfEditorService _pdfEditorService;
        private readonly IUiPlatformServices _platformServices;
        
        private string _filePath = string.Empty;
        private bool _isLoading;
        private bool _isExporting;
        private string _statusText = string.Empty;
        private string _exportSuccessMessage = string.Empty;
        
        private string _replaceTargetPage = string.Empty;
        private string _replaceSourcePage = string.Empty;
        private string _insertBlankPageIndex = string.Empty;

        public ObservableCollection<PdfPageViewModel> Pages { get; } = new ObservableCollection<PdfPageViewModel>();

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsExporting
        {
            get => _isExporting;
            set => SetProperty(ref _isExporting, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string ExportSuccessMessage
        {
            get => _exportSuccessMessage;
            set => SetProperty(ref _exportSuccessMessage, value);
        }

        public string ReplaceTargetPage
        {
            get => _replaceTargetPage;
            set { SetProperty(ref _replaceTargetPage, value); ReplacePageCommand?.RaiseCanExecuteChanged(); }
        }

        public string ReplaceSourcePage
        {
            get => _replaceSourcePage;
            set { SetProperty(ref _replaceSourcePage, value); ReplacePageCommand?.RaiseCanExecuteChanged(); }
        }

        public string InsertBlankPageIndex
        {
            get => _insertBlankPageIndex;
            set { SetProperty(ref _insertBlankPageIndex, value); InsertBlankPageCommand?.RaiseCanExecuteChanged(); }
        }

        public AsyncRelayCommand BrowsePdfCommand { get; }
        public AsyncRelayCommand DeleteSelectedPagesCommand { get; }
        public AsyncRelayCommand ExtractSelectedPagesCommand { get; }
        public AsyncRelayCommand ReverseOrderCommand { get; }
        public AsyncRelayCommand ReplacePageCommand { get; }
        public AsyncRelayCommand DuplicateSelectedCommand { get; }
        public AsyncRelayCommand InsertBlankPageCommand { get; }

        public PdfEditorViewModel(PdfEditorService pdfEditorService, IUiPlatformServices platformServices)
        {
            _pdfEditorService = pdfEditorService;
            _platformServices = platformServices;

            BrowsePdfCommand = new AsyncRelayCommand(BrowsePdfAsync);
            DeleteSelectedPagesCommand = new AsyncRelayCommand(DeleteSelectedPagesAsync, CanUseSelectedPages);
            ExtractSelectedPagesCommand = new AsyncRelayCommand(ExtractSelectedPagesAsync, CanUseSelectedPages);
            ReverseOrderCommand = new AsyncRelayCommand(ReverseOrderAsync, () => Pages.Count > 0 && !IsExporting);
            ReplacePageCommand = new AsyncRelayCommand(ReplacePageAsync, CanReplacePage);
            DuplicateSelectedCommand = new AsyncRelayCommand(DuplicateSelectedAsync, CanUseSelectedPages);
            InsertBlankPageCommand = new AsyncRelayCommand(InsertBlankPageAsync, CanInsertBlankPage);
        }

        private void RaiseAllCanExecuteChanged()
        {
            DeleteSelectedPagesCommand?.RaiseCanExecuteChanged();
            ExtractSelectedPagesCommand?.RaiseCanExecuteChanged();
            ReverseOrderCommand?.RaiseCanExecuteChanged();
            ReplacePageCommand?.RaiseCanExecuteChanged();
            DuplicateSelectedCommand?.RaiseCanExecuteChanged();
            InsertBlankPageCommand?.RaiseCanExecuteChanged();
        }

        private async Task BrowsePdfAsync()
        {
            var result = await _platformServices.PickPdfFileAsync();
            if (string.IsNullOrEmpty(result))
                return;

            FilePath = result;
            await LoadPdfPagesAsync();
        }

        private async Task LoadPdfPagesAsync()
        {
            IsLoading = true;
            Pages.Clear();
            StatusText = "Loading PDF...";
            ExportSuccessMessage = string.Empty;

            try
            {
                await Task.Run(() =>
                {
                    int pageCount = _pdfEditorService.GetPageCount(FilePath);
                    for (int i = 0; i < pageCount; i++)
                    {
                        var image = _pdfEditorService.GetPageThumbnail(FilePath, i);
                        var vm = new PdfPageViewModel(image);
                        
                        vm.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(PdfPageViewModel.IsSelected))
                            {
                                _platformServices.PostToUi(() => RaiseAllCanExecuteChanged());
                            }
                        };
                        
                        _platformServices.PostToUi(() => Pages.Add(vm));
                    }
                });
                StatusText = $"Loaded {Pages.Count} pages.";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                StatusText = $"Error loading PDF: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
                RaiseAllCanExecuteChanged();
            }
        }

        private bool CanUseSelectedPages()
        {
            return Pages.Any(p => p.IsSelected) && !IsExporting;
        }

        private bool CanReplacePage()
        {
            return !IsExporting && Pages.Count > 0 &&
                   int.TryParse(ReplaceTargetPage, out int t) && t >= 1 && t <= Pages.Count &&
                   int.TryParse(ReplaceSourcePage, out int s) && s >= 1 && s <= Pages.Count &&
                   t != s;
        }

        private bool CanInsertBlankPage()
        {
            return !IsExporting && Pages.Count > 0 &&
                   int.TryParse(InsertBlankPageIndex, out int idx) && idx >= 1 && idx <= Pages.Count + 1;
        }

        private async Task DeleteSelectedPagesAsync()
        {
            var selectedPages = Pages.Where(p => p.IsSelected).Select(p => p.PageNumber).ToList();
            if (!selectedPages.Any())
                return;

            IsExporting = true;
            DeleteSelectedPagesCommand.RaiseCanExecuteChanged();
            StatusText = $"Exporting PDF, removing {selectedPages.Count} pages...";
            ExportSuccessMessage = string.Empty;

            try
            {
                var outputPath = await Task.Run(() => _pdfEditorService.DeletePagesAndExport(FilePath, selectedPages));
                ExportSuccessMessage = $"Saved to: {outputPath}";
                StatusText = "Export successful.";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                StatusText = $"Error exporting PDF: {ex.Message}";
            }
            finally
            {
                IsExporting = false;
                RaiseAllCanExecuteChanged();
            }
        }

        private async Task ExtractSelectedPagesAsync()
        {
            await RunExportActionAsync(
                "Exporting selected pages...",
                () => _pdfEditorService.ExtractPagesAndExport(FilePath, Pages.Where(p => p.IsSelected).Select(p => p.PageNumber))
            );
        }

        private async Task ReverseOrderAsync()
        {
            await RunExportActionAsync(
                "Reversing page order...",
                () => _pdfEditorService.ReverseOrderAndExport(FilePath)
            );
        }

        private async Task ReplacePageAsync()
        {
            int target = int.Parse(ReplaceTargetPage);
            int source = int.Parse(ReplaceSourcePage);
            await RunExportActionAsync(
                $"Replacing page {target} with page {source}...",
                () => _pdfEditorService.ReplacePageAndExport(FilePath, target, source)
            );
        }

        private async Task DuplicateSelectedAsync()
        {
            await RunExportActionAsync(
                "Duplicating selected pages...",
                () => _pdfEditorService.DuplicatePagesAndExport(FilePath, Pages.Where(p => p.IsSelected).Select(p => p.PageNumber))
            );
        }

        private async Task InsertBlankPageAsync()
        {
            int idx = int.Parse(InsertBlankPageIndex);
            await RunExportActionAsync(
                $"Inserting blank page at {idx}...",
                () => _pdfEditorService.InsertBlankPageAndExport(FilePath, idx)
            );
        }

        private async Task RunExportActionAsync(string statusText, Func<string> exportFunc)
        {
            IsExporting = true;
            RaiseAllCanExecuteChanged();
            StatusText = statusText;
            ExportSuccessMessage = string.Empty;

            try
            {
                var outputPath = await Task.Run(exportFunc);
                ExportSuccessMessage = $"Saved to: {outputPath}";
                StatusText = "Export successful.";
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                StatusText = $"Error exporting PDF: {ex.Message}";
            }
            finally
            {
                IsExporting = false;
                RaiseAllCanExecuteChanged();
            }
        }
    }
}
