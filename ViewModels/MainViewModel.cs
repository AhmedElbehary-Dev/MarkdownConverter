using MarkdownConverter.Converters;
using MarkdownConverter.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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
        Success,
        Error
    }

    public class MainViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _toastTimer;
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
        private readonly ConversionService _conversionService = new();

        public MainViewModel()
        {
            Formats = new ObservableCollection<FormatOption>
            {
                new FormatOption("PDF", OutputFormat.Pdf, "pdf"),
                new FormatOption("Word", OutputFormat.Word, "docx"),
                new FormatOption("Excel", OutputFormat.Excel, "xlsx")
            };
            SelectedFormat = Formats.First().Value;

            BrowseCommand = new RelayCommand(_ => BrowseInputFile(), _ => !IsBusy);
            BrowseOutputFolderCommand = new RelayCommand(_ => BrowseOutputFolder(), _ => !IsBusy);
            OpenOutputFolderCommand = new RelayCommand(_ => OpenOutputFolder(), _ => !IsBusy);
            CopyOutputCommand = new RelayCommand(_ => CopyOutputPath(), _ => !string.IsNullOrWhiteSpace(OutputFilePath));
            ClearCommand = new RelayCommand(_ => Clear(), _ => !IsBusy);
            ConvertCommand = new AsyncRelayCommand(ConvertAsync, CanConvert);

            _toastTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _toastTimer.Tick += (_, _) =>
            {
                IsToastVisible = false;
                _toastTimer.Stop();
            };

            UpdateOutputPath();
        }

        public string AppTitle => "Markdown Converter Pro";

        public string AppVersion => "v1";

        public ObservableCollection<FormatOption> Formats { get; }

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

        public bool HasMarkdownFile => !string.IsNullOrWhiteSpace(SelectedMarkdownPath);

        public string OutputFolder
        {
            get => _outputFolder;
            set
            {
                if (SetProperty(ref _outputFolder, value))
                {
                    UpdateOutputPath();
                }
            }
        }

        public string OutputFilePath
        {
            get => _outputFilePath;
            private set
            {
                if (SetProperty(ref _outputFilePath, value))
                {
                    CopyOutputCommand.RaiseCanExecuteChanged();
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

        public RelayCommand BrowseCommand { get; }

        public RelayCommand BrowseOutputFolderCommand { get; }

        public RelayCommand OpenOutputFolderCommand { get; }

        public RelayCommand CopyOutputCommand { get; }

        public RelayCommand ClearCommand { get; }

        public AsyncRelayCommand ConvertCommand { get; }

        public void SetSelectedMarkdownPath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            SelectedMarkdownPath = filePath;
            if (!_outputFolderManuallySet)
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    OutputFolder = directory;
                }
            }

            StatusText = "Ready";
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

        private void BrowseInputFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Markdown Files (*.md;*.markdown)|*.md;*.markdown|All Files (*.*)|*.*",
                Title = "Select Markdown File"
            };

            if (dialog.ShowDialog() == true)
            {
                SetSelectedMarkdownPath(dialog.FileName);
            }
        }

        private void BrowseOutputFolder()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select output folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true,
                SelectedPath = Directory.Exists(OutputFolder) ? OutputFolder : string.Empty
            };

            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                _outputFolderManuallySet = true;
                OutputFolder = dialog.SelectedPath;
            }
        }

        private void OpenOutputFolder()
        {
            if (string.IsNullOrWhiteSpace(OutputFolder) || !Directory.Exists(OutputFolder))
            {
                ShowToast("Output folder is not available.", ToastKind.Error);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = OutputFolder,
                UseShellExecute = true
            });
        }

        private void CopyOutputPath()
        {
            if (string.IsNullOrWhiteSpace(OutputFilePath))
            {
                return;
            }

            System.Windows.Clipboard.SetText(OutputFilePath);
            ShowToast("Output path copied.", ToastKind.Success);
        }

        private void Clear()
        {
            SelectedMarkdownPath = string.Empty;
            OutputFilePath = string.Empty;
            Progress = 0;
            StatusText = "Ready";
            StatusKind = StatusKind.Neutral;
        }

        private bool CanConvert()
        {
            return !IsBusy && File.Exists(SelectedMarkdownPath);
        }

        private async Task ConvertAsync()
        {
            if (!File.Exists(SelectedMarkdownPath))
            {
                StatusText = "Select a valid markdown file.";
                StatusKind = StatusKind.Error;
                ShowToast("Please select a valid markdown file.", ToastKind.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(OutputFilePath))
            {
                StatusText = "Choose an output folder.";
                StatusKind = StatusKind.Error;
                ShowToast("Output folder is required.", ToastKind.Error);
                return;
            }

            if (File.Exists(OutputFilePath) && !OverwriteIfExists)
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

            var formatValue = GetSelectedExtension();
            var progressReporter = new Progress<double>(value => Progress = Math.Clamp(value, 0, 100));

            try
            {
                await _conversionService.ConvertAsync(SelectedMarkdownPath, OutputFilePath, formatValue, progressReporter).ConfigureAwait(true);
                Progress = 100;
                StatusText = "Complete";
                StatusKind = StatusKind.Success;
                ShowAlertPopup("Conversion succeeded.", ToastKind.Success);

                if (OpenAfterConvert)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = OutputFilePath,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        ShowToast("Could not open the output file.", ToastKind.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = "Conversion failed.";
                StatusKind = StatusKind.Error;
                ShowAlertPopup($"Conversion failed:{Environment.NewLine}{ex}", ToastKind.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void UpdateOutputPath()
        {
            if (string.IsNullOrWhiteSpace(SelectedMarkdownPath))
            {
                OutputFilePath = string.Empty;
                return;
            }

            var baseFolder = string.IsNullOrWhiteSpace(OutputFolder)
                ? Path.GetDirectoryName(SelectedMarkdownPath) ?? Environment.CurrentDirectory
                : OutputFolder;

            var fileName = Path.GetFileNameWithoutExtension(SelectedMarkdownPath);
            var extension = GetSelectedExtension();
            OutputFilePath = Path.Combine(baseFolder, $"{fileName}.{extension}");
        }

        private string GetSelectedExtension()
        {
            var option = Formats.FirstOrDefault(item => item.Value == SelectedFormat);
            return option?.Extension ?? "pdf";
        }

        private void ShowAlertPopup(string message, ToastKind kind)
        {
            var owner = System.Windows.Application.Current?.MainWindow;
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                var dialog = new MarkdownConverter.UI.MessageDialog(message, kind)
                {
                    Owner = owner
                };
                dialog.ShowDialog();
            });
        }

        private void ShowToast(string message, ToastKind kind)
        {
            ToastMessage = message;
            ToastKind = kind;
            IsToastVisible = true;
            _toastTimer.Stop();
            _toastTimer.Start();
        }

        private void RaiseCommandStates()
        {
            BrowseCommand.RaiseCanExecuteChanged();
            BrowseOutputFolderCommand.RaiseCanExecuteChanged();
            OpenOutputFolderCommand.RaiseCanExecuteChanged();
            ClearCommand.RaiseCanExecuteChanged();
        }
    }
}
