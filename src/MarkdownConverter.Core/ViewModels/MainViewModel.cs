using MarkdownConverter.Converters;
using MarkdownConverter.Models;
using MarkdownConverter.Platform;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Reflection;

namespace MarkdownConverter.ViewModels
{
    public enum StatusKind
    {
        Neutral,
        Info,
        Success,
        Error
    }

    public enum ToastKind
    {
        Neutral,
        Success,
        Error
    }

    public class MainViewModel : ViewModelBase
    {
        private readonly IUiPlatformServices _uiPlatformServices;
        private readonly ConversionService _conversionService;
        private readonly IUiTimer _toastTimer;
        private readonly AsyncRelayCommand _browseCommand;
        private readonly AsyncRelayCommand _browseOutputFolderCommand;
        private readonly RelayCommand _openOutputFolderCommand;
        private readonly RelayCommand _copyOutputCommand;
        private readonly RelayCommand _clearCommand;

        private string _selectedMarkdownPath = string.Empty;
        private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private string _outputFilePath = string.Empty;
        private OutputFormat _selectedFormat;
        private bool _openAfterConvert = true;
        private bool _overwriteIfExists;
        private bool _preserveStructure = true;
        private string _statusText = "Ready";
        private StatusKind _statusKind = StatusKind.Neutral;
        private bool _isBusy;
        private double _progress;
        private bool _isToastVisible;
        private string _toastMessage = string.Empty;
        private ToastKind _toastKind = ToastKind.Success;
        private bool _outputFolderManuallySet;
        private bool _isDropZoneActive;

        public MainViewModel(ConversionService conversionService, IUiPlatformServices uiPlatformServices)
        {
            _conversionService = conversionService ?? throw new ArgumentNullException(nameof(conversionService));
            _uiPlatformServices = uiPlatformServices ?? throw new ArgumentNullException(nameof(uiPlatformServices));

            Formats = new ObservableCollection<FormatOption>
            {
                new FormatOption("PDF", OutputFormat.Pdf, "pdf"),
                new FormatOption("Word", OutputFormat.Word, "docx"),
                new FormatOption("Excel", OutputFormat.Excel, "xlsx")
            };
            SelectedFormat = Formats.First().Value;

            SelectedFiles = new ObservableCollection<string>();
            SelectedFiles.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasMarkdownFile));
                OnPropertyChanged(nameof(HasMultipleFiles));
                OnPropertyChanged(nameof(SelectedFileCount));
                OnPropertyChanged(nameof(SelectedFilesDisplayText));
                ConvertCommand?.RaiseCanExecuteChanged();
            };

            _browseCommand = new AsyncRelayCommand(BrowseInputFileAsync, () => !IsBusy);
            _browseOutputFolderCommand = new AsyncRelayCommand(BrowseOutputFolderAsync, () => !IsBusy);
            _openOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder(), _ => !IsBusy);
            _copyOutputCommand = new RelayCommand(_ => CopyOutputPath(), _ => !string.IsNullOrWhiteSpace(OutputFilePath));
            _clearCommand = new RelayCommand(_ => Clear(), _ => !IsBusy);

            BrowseCommand = _browseCommand;
            BrowseOutputFolderCommand = _browseOutputFolderCommand;
            OpenOutputFolderCommand = _openOutputFolderCommand;
            CopyOutputCommand = _copyOutputCommand;
            ClearCommand = _clearCommand;
            ConvertCommand = new AsyncRelayCommand(ConvertAsync, CanConvert);
            RemoveFileCommand = new RelayCommand(RemoveFile, _ => !IsBusy);

            _toastTimer = _uiPlatformServices.CreateTimer(TimeSpan.FromSeconds(3), OnToastTimerTick);

            UpdateOutputPath();
        }

        public string AppTitle => "Markdown Converter Pro";

        public string AppVersion
        {
            get
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var version = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                
                if (string.IsNullOrWhiteSpace(version))
                {
                    version = asm.GetName().Version?.ToString(3);
                }
                else
                {
                    int plusIndex = version.IndexOf('+');
                    if (plusIndex > 0)
                    {
                        version = version.Substring(0, plusIndex);
                    }
                }

                return string.IsNullOrEmpty(version) ? "v1.0.0" : (version.StartsWith("v") ? version : "v" + version);
            }
        }

        public ObservableCollection<FormatOption> Formats { get; }

        /// <summary>
        /// The list of selected markdown files for batch conversion.
        /// </summary>
        public ObservableCollection<string> SelectedFiles { get; }

        /// <summary>
        /// True when more than one file is selected.
        /// </summary>
        public bool HasMultipleFiles => SelectedFiles.Count > 1;

        /// <summary>
        /// Number of files currently queued for conversion.
        /// </summary>
        public int SelectedFileCount => SelectedFiles.Count;

        /// <summary>
        /// Display text for the drop zone: shows the single file path, or "N files selected".
        /// </summary>
        public string SelectedFilesDisplayText
        {
            get
            {
                if (SelectedFiles.Count == 0)
                {
                    return string.Empty;
                }

                if (SelectedFiles.Count == 1)
                {
                    return SelectedFiles[0];
                }

                return $"{SelectedFiles.Count} files selected";
            }
        }

        public OutputFormat SelectedFormat
        {
            get => _selectedFormat;
            set
            {
                if (SetProperty(ref _selectedFormat, value))
                {
                    OnPropertyChanged(nameof(SelectedFormatLabel));
                    UpdateOutputPath();
                }
            }
        }

        public string SelectedFormatLabel => Formats.FirstOrDefault(option => option.Value == SelectedFormat)?.Label ?? "PDF";

        public string SelectedMarkdownPath
        {
            get => _selectedMarkdownPath;
            set
            {
                if (SetProperty(ref _selectedMarkdownPath, value))
                {
                    OnPropertyChanged(nameof(HasMarkdownFile));
                    UpdateOutputPath();
                    RaiseCommandStates();
                    ConvertCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HasMarkdownFile => SelectedFiles.Count > 0 || !string.IsNullOrWhiteSpace(SelectedMarkdownPath);

        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                SetOutputFolder(value, markAsManual: true);
            }
        }

        public string OutputFilePath
        {
            get => _outputFilePath;
            private set
            {
                if (SetProperty(ref _outputFilePath, value))
                {
                    _copyOutputCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool OpenAfterConvert
        {
            get => _openAfterConvert;
            set => SetProperty(ref _openAfterConvert, value);
        }

        public bool OverwriteIfExists
        {
            get => _overwriteIfExists;
            set => SetProperty(ref _overwriteIfExists, value);
        }

        public bool PreserveStructure
        {
            get => _preserveStructure;
            set => SetProperty(ref _preserveStructure, value);
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

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    RaiseCommandStates();
                    ConvertCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public double Progress
        {
            get => _progress;
            private set => SetProperty(ref _progress, value);
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

        public bool IsDropZoneActive
        {
            get => _isDropZoneActive;
            set => SetProperty(ref _isDropZoneActive, value);
        }

        public ICommand BrowseCommand { get; }

        public ICommand BrowseOutputFolderCommand { get; }

        public RelayCommand OpenOutputFolderCommand { get; }

        public RelayCommand CopyOutputCommand { get; }

        public RelayCommand ClearCommand { get; }

        public AsyncRelayCommand ConvertCommand { get; }

        /// <summary>
        /// Command to remove an individual file from the selection. Parameter is the file path string.
        /// </summary>
        public RelayCommand RemoveFileCommand { get; }

        public void SetSelectedMarkdownPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            SelectedMarkdownPath = filePath;

            // Sync SelectedFiles to contain just this single file
            SelectedFiles.Clear();
            SelectedFiles.Add(filePath);

            if (!_outputFolderManuallySet)
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    SetOutputFolder(directory, markAsManual: false);
                }
            }

            StatusText = "Ready";
            StatusKind = StatusKind.Neutral;
        }

        /// <summary>
        /// Set multiple markdown file paths for batch conversion.
        /// Replaces any existing selection.
        /// </summary>
        public void SetSelectedMarkdownPaths(string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
            {
                return;
            }

            // Validate and filter to only valid markdown files
            var validPaths = filePaths.Where(p => IsValidMarkdownFile(p) && File.Exists(p)).ToArray();

            if (validPaths.Length == 0)
            {
                StatusText = "No valid markdown files found.";
                StatusKind = StatusKind.Error;
                ShowToast("No valid .md or .markdown files in selection.", ToastKind.Error);
                return;
            }

            SelectedFiles.Clear();
            foreach (var path in validPaths)
            {
                SelectedFiles.Add(path);
            }

            // Keep SelectedMarkdownPath in sync for backward compatibility
            SelectedMarkdownPath = validPaths[0];

            if (!_outputFolderManuallySet)
            {
                var directory = Path.GetDirectoryName(validPaths[0]);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    SetOutputFolder(directory, markAsManual: false);
                }
            }

            int skipped = filePaths.Length - validPaths.Length;
            if (skipped > 0)
            {
                ShowToast($"{validPaths.Length} files added, {skipped} skipped (not .md/.markdown).", ToastKind.Success);
            }

            StatusText = validPaths.Length == 1 ? "Ready" : $"{validPaths.Length} files ready";
            StatusKind = StatusKind.Neutral;
        }

        public bool IsValidMarkdownFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            var extension = Path.GetExtension(filePath);
            return string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase);
        }

        public bool TrySetSelectedMarkdownPathFromDrop(string filePath)
        {
            if (!IsValidMarkdownFile(filePath))
            {
                StatusText = "Unsupported file type.";
                StatusKind = StatusKind.Error;
                ShowToast("Only .md or .markdown files are supported.", ToastKind.Error);
                return false;
            }

            SetSelectedMarkdownPath(filePath);
            return true;
        }

        /// <summary>
        /// Accept multiple dropped files. Validates each and adds valid ones.
        /// </summary>
        public bool TrySetSelectedMarkdownPathsFromDrop(string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
            {
                return false;
            }

            // Single file drop — use existing path for identical UX
            if (filePaths.Length == 1)
            {
                return TrySetSelectedMarkdownPathFromDrop(filePaths[0]);
            }

            SetSelectedMarkdownPaths(filePaths);
            return SelectedFiles.Count > 0;
        }

        private async Task BrowseInputFileAsync()
        {
            var selected = await _uiPlatformServices.PickMarkdownFilesAsync().ConfigureAwait(true);
            if (selected != null && selected.Length > 0)
            {
                if (selected.Length == 1)
                {
                    SetSelectedMarkdownPath(selected[0]);
                }
                else
                {
                    SetSelectedMarkdownPaths(selected);
                }
            }
        }

        private async Task BrowseOutputFolderAsync()
        {
            var selected = await _uiPlatformServices.PickOutputFolderAsync(Directory.Exists(OutputFolder) ? OutputFolder : string.Empty)
                .ConfigureAwait(true);

            if (!string.IsNullOrWhiteSpace(selected))
            {
                _outputFolderManuallySet = true;
                SetOutputFolder(selected, markAsManual: true);
            }
        }

        private void OpenOutputFolder()
        {
            if (string.IsNullOrWhiteSpace(OutputFolder) || !Directory.Exists(OutputFolder))
            {
                ShowToast("Output folder is not available.", ToastKind.Error);
                return;
            }

            _uiPlatformServices.OpenPathWithShell(OutputFolder);
        }

        private void CopyOutputPath()
        {
            if (string.IsNullOrWhiteSpace(OutputFilePath))
            {
                return;
            }

            _uiPlatformServices.SetClipboardText(OutputFilePath);
            ShowToast("Output path copied.", ToastKind.Success);
        }

        private void Clear()
        {
            SelectedFiles.Clear();
            SelectedMarkdownPath = string.Empty;
            OutputFilePath = string.Empty;
            Progress = 0;
            StatusText = "Ready";
            StatusKind = StatusKind.Neutral;
        }

        private bool CanConvert()
        {
            if (IsBusy)
            {
                return false;
            }

            // Multi-file: at least one file must exist
            if (SelectedFiles.Count > 0)
            {
                return SelectedFiles.Any(File.Exists);
            }

            // Fallback: single file
            return File.Exists(SelectedMarkdownPath);
        }

        private async Task ConvertAsync()
        {
            var filesToConvert = SelectedFiles.Count > 0
                ? SelectedFiles.Where(File.Exists).ToArray()
                : (File.Exists(SelectedMarkdownPath) ? new[] { SelectedMarkdownPath } : Array.Empty<string>());

            if (filesToConvert.Length == 0)
            {
                StatusText = "Select a valid markdown file.";
                StatusKind = StatusKind.Error;
                ShowToast("Please select a valid markdown file.", ToastKind.Error);
                return;
            }

            // Single-file conversion — use original logic for identical behavior
            if (filesToConvert.Length == 1)
            {
                await ConvertSingleFileAsync(filesToConvert[0]).ConfigureAwait(true);
                return;
            }

            // Multi-file batch conversion
            await ConvertMultipleFilesAsync(filesToConvert).ConfigureAwait(true);
        }

        private async Task ConvertSingleFileAsync(string inputPath)
        {
            var outputPath = BuildOutputPath(inputPath);

            if (string.IsNullOrWhiteSpace(outputPath))
            {
                StatusText = "Choose an output folder.";
                StatusKind = StatusKind.Error;
                ShowToast("Output folder is required.", ToastKind.Error);
                return;
            }

            if (File.Exists(outputPath) && !OverwriteIfExists)
            {
                StatusText = "Output file already exists.";
                StatusKind = StatusKind.Error;
                ShowToast("Output file already exists.", ToastKind.Error);
                return;
            }

            IsBusy = true;
            Progress = 0;
            StatusText = "Converting...";
            StatusKind = StatusKind.Info;
            OutputFilePath = outputPath;

            var formatValue = GetSelectedExtension();
            var progressReporter = new Progress<double>(value => Progress = Math.Clamp(value, 0, 100));

            try
            {
                await _conversionService.ConvertAsync(inputPath, outputPath, formatValue, progressReporter).ConfigureAwait(true);
                Progress = 100;
                StatusText = "Complete";
                StatusKind = StatusKind.Success;
                await ShowAlertPopupAsync("Conversion succeeded.", ToastKind.Success).ConfigureAwait(true);

                if (OpenAfterConvert)
                {
                    try
                    {
                        _uiPlatformServices.OpenPathWithShell(outputPath);
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or System.IO.IOException)
                    {
                        ShowToast("Could not open the output file.", ToastKind.Error);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                StatusText = "Conversion failed.";
                StatusKind = StatusKind.Error;
                await ShowAlertPopupAsync($"Conversion failed:{Environment.NewLine}{ex}", ToastKind.Error).ConfigureAwait(true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ConvertMultipleFilesAsync(string[] filePaths)
        {
            IsBusy = true;
            Progress = 0;
            StatusText = $"Converting 0/{filePaths.Length}...";
            StatusKind = StatusKind.Info;

            var formatValue = GetSelectedExtension();
            int succeeded = 0;
            int failed = 0;
            string lastOutputPath = string.Empty;

            try
            {
                for (int i = 0; i < filePaths.Length; i++)
                {
                    var inputPath = filePaths[i];
                    var outputPath = BuildOutputPath(inputPath);

                    if (string.IsNullOrWhiteSpace(outputPath))
                    {
                        failed++;
                        continue;
                    }

                    if (File.Exists(outputPath) && !OverwriteIfExists)
                    {
                        failed++;
                        continue;
                    }

                    StatusText = $"Converting {i + 1}/{filePaths.Length}...";
                    OutputFilePath = outputPath;

                    // Calculate progress: each file gets an equal slice of 0-100
                    double fileBaseProgress = (double)i / filePaths.Length * 100;
                    double fileSlice = 100.0 / filePaths.Length;
                    var progressReporter = new Progress<double>(value =>
                    {
                        double clampedFileProgress = Math.Clamp(value, 0, 100);
                        Progress = Math.Clamp(fileBaseProgress + (clampedFileProgress / 100.0 * fileSlice), 0, 100);
                    });

                    try
                    {
                        await _conversionService.ConvertAsync(inputPath, outputPath, formatValue, progressReporter).ConfigureAwait(true);
                        succeeded++;
                        lastOutputPath = outputPath;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        failed++;
                        // Continue with remaining files instead of aborting
                    }
                }

                Progress = 100;

                if (failed == 0)
                {
                    StatusText = $"Complete ({succeeded} files)";
                    StatusKind = StatusKind.Success;
                    await ShowAlertPopupAsync($"All {succeeded} files converted successfully.", ToastKind.Success).ConfigureAwait(true);
                }
                else if (succeeded > 0)
                {
                    StatusText = $"Partial ({succeeded} ok, {failed} failed)";
                    StatusKind = StatusKind.Error;
                    await ShowAlertPopupAsync($"{succeeded} files converted, {failed} failed.", ToastKind.Error).ConfigureAwait(true);
                }
                else
                {
                    StatusText = "All conversions failed.";
                    StatusKind = StatusKind.Error;
                    await ShowAlertPopupAsync("All conversions failed.", ToastKind.Error).ConfigureAwait(true);
                }

                // For multi-file, open the output folder rather than individual files
                if (OpenAfterConvert && succeeded > 0 && !string.IsNullOrWhiteSpace(OutputFolder) && Directory.Exists(OutputFolder))
                {
                    try
                    {
                        _uiPlatformServices.OpenPathWithShell(OutputFolder);
                    }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or System.IO.IOException)
                    {
                        ShowToast("Could not open the output folder.", ToastKind.Error);
                    }
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Build the output file path for a given input markdown file.
        /// </summary>
        private string BuildOutputPath(string inputPath)
        {
            var baseFolder = string.IsNullOrWhiteSpace(OutputFolder)
                ? Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory
                : OutputFolder;

            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            var extension = GetSelectedExtension();
            return Path.Join(baseFolder, $"{fileName}.{extension}");
        }

        private void UpdateOutputPath()
        {
            if (string.IsNullOrWhiteSpace(SelectedMarkdownPath))
            {
                // For multi-file, show the output folder path
                if (SelectedFiles.Count > 1)
                {
                    OutputFilePath = OutputFolder;
                }
                else
                {
                    OutputFilePath = string.Empty;
                }
                return;
            }

            OutputFilePath = BuildOutputPath(SelectedMarkdownPath);
        }

        private void SetOutputFolder(string value, bool markAsManual)
        {
            if (SetProperty(ref _outputFolder, value))
            {
                if (markAsManual)
                {
                    _outputFolderManuallySet = true;
                }

                UpdateOutputPath();
            }
        }

        private string GetSelectedExtension()
        {
            var option = Formats.FirstOrDefault(item => item.Value == SelectedFormat);
            return option?.Extension ?? "pdf";
        }

        private Task ShowAlertPopupAsync(string message, ToastKind kind)
        {
            return _uiPlatformServices.ShowMessageAsync(message, kind);
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
            _browseCommand.RaiseCanExecuteChanged();
            _browseOutputFolderCommand.RaiseCanExecuteChanged();
            _openOutputFolderCommand.RaiseCanExecuteChanged();
            _clearCommand.RaiseCanExecuteChanged();
        }

        private void RemoveFile(object? parameter)
        {
            if (parameter is string filePath && SelectedFiles.Contains(filePath))
            {
                SelectedFiles.Remove(filePath);

                // Update SelectedMarkdownPath to reflect current state
                if (SelectedFiles.Count > 0)
                {
                    _selectedMarkdownPath = SelectedFiles[0];
                    OnPropertyChanged(nameof(SelectedMarkdownPath));
                }
                else
                {
                    SelectedMarkdownPath = string.Empty;
                }

                UpdateOutputPath();
                StatusText = SelectedFiles.Count > 0
                    ? (SelectedFiles.Count == 1 ? "Ready" : $"{SelectedFiles.Count} files ready")
                    : "Ready";
                StatusKind = StatusKind.Neutral;
            }
        }
    }
}
