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

        public AsyncRelayCommand BrowsePdfCommand { get; }
        public AsyncRelayCommand DeleteSelectedPagesCommand { get; }

        public PdfEditorViewModel(PdfEditorService pdfEditorService, IUiPlatformServices platformServices)
        {
            _pdfEditorService = pdfEditorService;
            _platformServices = platformServices;

            BrowsePdfCommand = new AsyncRelayCommand(BrowsePdfAsync);
            DeleteSelectedPagesCommand = new AsyncRelayCommand(DeleteSelectedPagesAsync, CanDeleteSelectedPages);
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
                                _platformServices.PostToUi(() => DeleteSelectedPagesCommand.RaiseCanExecuteChanged());
                            }
                        };
                        
                        _platformServices.PostToUi(() => Pages.Add(vm));
                    }
                });
                StatusText = $"Loaded {Pages.Count} pages.";
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading PDF: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanDeleteSelectedPages()
        {
            return Pages.Any(p => p.IsSelected) && !IsExporting;
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
            catch (Exception ex)
            {
                StatusText = $"Error exporting PDF: {ex.Message}";
            }
            finally
            {
                IsExporting = false;
                DeleteSelectedPagesCommand.RaiseCanExecuteChanged();
            }
        }
    }
}
