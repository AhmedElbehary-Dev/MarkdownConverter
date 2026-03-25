using MarkdownConverter.Converters;
using MarkdownConverter.Models;
using MarkdownConverter.Platform;
using MarkdownConverter.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.Generic;

namespace MarkdownConverter.ViewModels
{
    public class QuickPasteViewModel : ViewModelBase
    {
        private readonly IUiPlatformServices _uiPlatformServices;
        private readonly ConversionService _conversionService;
        private readonly QuickPasteStorageService _storageService;
        private readonly IUiTimer _toastTimer;

        private string _title = string.Empty;
        private string _markdownContent = string.Empty;
        private OutputFormat _selectedFormat;
        private QuickPasteEntry? _selectedHistoryEntry;
        private string _searchQuery = string.Empty;
        private bool _isBusy;
        private string _statusText = "Ready";
        private StatusKind _statusKind = StatusKind.Neutral;
        private bool _isToastVisible;
        private string _toastMessage = string.Empty;
        private ToastKind _toastKind = ToastKind.Success;

        private List<QuickPasteEntry> _allHistory = new();

        public QuickPasteViewModel(
            ConversionService conversionService, 
            IUiPlatformServices uiPlatformServices,
            QuickPasteStorageService storageService)
        {
            _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
            _uiPlatformServices = uiPlatformServices ?? throw new ArgumentNullException(nameof(uiPlatformServices));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

            Formats = new ObservableCollection<FormatOption>
            {
                new FormatOption("PDF", OutputFormat.Pdf, "pdf"),
                new FormatOption("Word", OutputFormat.Word, "docx"),
                new FormatOption("Excel", OutputFormat.Excel, "xlsx")
            };
            SelectedFormat = Formats.First().Value;

            SaveCommand = new AsyncRelayCommand(SaveAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(MarkdownContent));
            SaveAndExportCommand = new AsyncRelayCommand(SaveAndExportAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(MarkdownContent));
            LoadEntryCommand = new AsyncRelayCommand<QuickPasteEntry>(LoadEntryAsync, _ => !IsBusy);
            DeleteEntryCommand = new AsyncRelayCommand<QuickPasteEntry>(DeleteEntryAsync, _ => !IsBusy);
            ClearEditorCommand = new RelayCommand(_ => ClearEditor(), _ => !IsBusy);
            PasteFromClipboardCommand = new AsyncRelayCommand(PasteFromClipboardAsync, () => !IsBusy);
            SearchCommand = new RelayCommand(_ => FilterHistory(), _ => true);

            _toastTimer = _uiPlatformServices.CreateTimer(TimeSpan.FromSeconds(3), OnToastTimerTick);

            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadHistoryAsync();
            await CheckClipboardForAutoFillAsync();
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string MarkdownContent
        {
            get => _markdownContent;
            set
            {
                if (SetProperty(ref _markdownContent, value))
                {
                    if (SelectedHistoryEntry == null && string.IsNullOrWhiteSpace(Title))
                    {
                        Title = _storageService.ExtractTitle(value);
                    }
                    RaiseCommandStates();
                }
            }
        }

        public ObservableCollection<FormatOption> Formats { get; }

        public OutputFormat SelectedFormat
        {
            get => _selectedFormat;
            set => SetProperty(ref _selectedFormat, value);
        }

        public ObservableCollection<QuickPasteEntry> History { get; } = new();

        public QuickPasteEntry? SelectedHistoryEntry
        {
            get => _selectedHistoryEntry;
            set => SetProperty(ref _selectedHistoryEntry, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    FilterHistory();
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCommandStates();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        public StatusKind StatusKind
        {
            get => _statusKind;
            private set => SetProperty(ref _statusKind, value);
        }

        public bool IsToastVisible
        {
            get => _isToastVisible;
            private set => SetProperty(ref _isToastVisible, value);
        }

        public string ToastMessage
        {
            get => _toastMessage;
            private set => SetProperty(ref _toastMessage, value);
        }

        public ToastKind ToastKind
        {
            get => _toastKind;
            private set => SetProperty(ref _toastKind, value);
        }

        public AsyncRelayCommand SaveCommand { get; }
        public AsyncRelayCommand SaveAndExportCommand { get; }
        public AsyncRelayCommand<QuickPasteEntry> LoadEntryCommand { get; }
        public AsyncRelayCommand<QuickPasteEntry> DeleteEntryCommand { get; }
        public RelayCommand ClearEditorCommand { get; }
        public AsyncRelayCommand PasteFromClipboardCommand { get; }
        public RelayCommand SearchCommand { get; }

        private async Task LoadHistoryAsync()
        {
            _allHistory = await _storageService.LoadHistoryAsync();
            FilterHistory();
        }

        private void FilterHistory()
        {
            History.Clear();
            var query = SearchQuery?.ToLowerInvariant() ?? "";
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allHistory
                : _allHistory.Where(e => e.Title.ToLowerInvariant().Contains(query) || e.FileName.ToLowerInvariant().Contains(query));

            foreach (var item in filtered)
            {
                History.Add(item);
            }
        }

        private async Task CheckClipboardForAutoFillAsync()
        {
            try
            {
                var text = await _uiPlatformServices.GetClipboardTextAsync();
                if (!string.IsNullOrWhiteSpace(text) && IsMarkdownLikely(text) && string.IsNullOrWhiteSpace(MarkdownContent))
                {
                    MarkdownContent = text;
                    ShowToast("Auto-filled from clipboard", ViewModels.ToastKind.Success);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { /* Ignore clipboard errors on load */ }
        }

        private bool IsMarkdownLikely(string text)
        {
            return text.Contains("# ") || text.Contains("```") || text.Contains("**") || text.Contains("- ") || text.Contains("* ");
        }

        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(MarkdownContent)) return;

            IsBusy = true;
            StatusText = "Saving...";
            
            try
            {
                var finalTitle = string.IsNullOrWhiteSpace(Title) ? _storageService.ExtractTitle(MarkdownContent) : Title;
                
                if (SelectedHistoryEntry != null)
                {
                    await _storageService.UpdateAsync(SelectedHistoryEntry.Id, finalTitle, MarkdownContent);
                    Title = finalTitle;
                    ShowToast("Updated successfuly", ViewModels.ToastKind.Success);
                }
                else
                {
                    var entry = await _storageService.SaveAsync(finalTitle, MarkdownContent);
                    SelectedHistoryEntry = entry;
                    Title = entry.Title;
                    ShowToast("Saved to history", ViewModels.ToastKind.Success);
                }
                
                await LoadHistoryAsync();
                StatusText = "Saved";
                StatusKind = StatusKind.Success;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                StatusText = "Save failed";
                StatusKind = StatusKind.Error;
                ShowToast("Failed to save: " + ex.Message, ViewModels.ToastKind.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveAndExportAsync()
        {
            await SaveAsync();
            if (SelectedHistoryEntry == null) return;

            var outputFolder = await _uiPlatformServices.PickOutputFolderAsync(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (string.IsNullOrWhiteSpace(outputFolder)) return;

            IsBusy = true;
            StatusText = "Converting...";
            StatusKind = StatusKind.Info;

            var ext = Formats.FirstOrDefault(f => f.Value == SelectedFormat)?.Extension ?? "pdf";
            var outputPath = Path.Join(outputFolder, $"{SelectedHistoryEntry.Title}.{ext}");

            try
            {
                // Write temp markdown file for ConversionService
                var tempMd = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempMd, MarkdownContent);

                await _conversionService.ConvertAsync(tempMd, outputPath, ext);
                File.Delete(tempMd);

                await _storageService.MarkExportedAsync(SelectedHistoryEntry.Id, ext);
                await LoadHistoryAsync();

                StatusText = "Converted successfully";
                StatusKind = StatusKind.Success;
                await _uiPlatformServices.ShowMessageAsync("Export successful! Saved to:\\n" + outputPath, ViewModels.ToastKind.Success);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                StatusText = "Export failed";
                StatusKind = StatusKind.Error;
                ShowToast("Failed to export: " + ex.Message, ViewModels.ToastKind.Error);
                await _uiPlatformServices.ShowMessageAsync("Export failed:\\n" + ex.Message, ViewModels.ToastKind.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadEntryAsync(QuickPasteEntry? entry)
        {
            if (entry == null) return;
            IsBusy = true;
            
            try
            {
                var content = await _storageService.LoadContentAsync(entry.Id);
                if (content != null)
                {
                    SelectedHistoryEntry = entry;
                    _markdownContent = content; // bypass property setter so it doesn't auto-extract title
                    OnPropertyChanged(nameof(MarkdownContent));
                    Title = entry.Title;
                    StatusText = "Loaded";
                    RaiseCommandStates();
                }
                else
                {
                    ShowToast("File not found", ViewModels.ToastKind.Error);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task DeleteEntryAsync(QuickPasteEntry? entry)
        {
            if (entry == null) return;
            
            await _storageService.DeleteAsync(entry.Id);
            await LoadHistoryAsync();
            
            if (SelectedHistoryEntry?.Id == entry.Id)
            {
                ClearEditor();
            }
            ShowToast("Deleted from history", ViewModels.ToastKind.Success);
        }

        private void ClearEditor()
        {
            SelectedHistoryEntry = null;
            MarkdownContent = string.Empty;
            Title = string.Empty;
            StatusText = "Ready";
            StatusKind = StatusKind.Neutral;
        }

        private async Task PasteFromClipboardAsync()
        {
            var text = await _uiPlatformServices.GetClipboardTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                MarkdownContent = text;
                ShowToast("Pasted from clipboard", ViewModels.ToastKind.Success);
            }
        }

        private void ShowToast(string message, ToastKind kind)
        {
            ToastMessage = message;
            ToastKind = kind;
            IsToastVisible = true;
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        private void OnToastTimerTick()
        {
            IsToastVisible = false;
            _toastTimer.Stop();
        }

        private void RaiseCommandStates()
        {
            SaveCommand.RaiseCanExecuteChanged();
            SaveAndExportCommand.RaiseCanExecuteChanged();
        }
    }
}
