using MarkdownConverter.Converters;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinFormsPanel = System.Windows.Forms.Panel;

namespace MarkdownConverter
{
    public partial class MainForm : PoisonForm
    {
        private string _selectedFilePath = string.Empty;
        private string _selectedFormat = "pdf";
        private readonly PoisonTextBox _filePathTextBox = new PoisonTextBox();
        private readonly PoisonComboBox _formatComboBox = new PoisonComboBox();
        private readonly PoisonButton _convertButton = new PoisonButton();
        private readonly PoisonButton _browseButton = new PoisonButton();
        private readonly PoisonLabel _statusLabel = new PoisonLabel();
        private readonly ToolTip _toolTip = new ToolTip();
        private readonly FormatOption[] _formatOptions = new[]
        {
            new FormatOption("PDF · Portable Document Format", "pdf"),
            new FormatOption("Word · Microsoft Word (.docx)", "docx"),
            new FormatOption("Excel · Microsoft Excel (.xlsx)", "xlsx")
        };

        public MainForm()
        {
            InitializeComponent();
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "Markdown Converter Pro";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(740, 560);
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            BackColor = Color.FromArgb(12, 16, 24);
            Padding = new Padding(0);
            AcceptButton = _convertButton;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            mainLayout.Controls.Add(CreateWindowHeader(), 0, 0);
            mainLayout.Controls.Add(CreateContentLayout(), 0, 1);

            Controls.Add(mainLayout);
            UpdateFormatSelection();
        }

        private TableLayoutPanel CreateContentLayout()
        {
            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(20, 14, 20, 18)
            };
            contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            contentLayout.Controls.Add(CreateHeroPanel(), 0, 0);
            contentLayout.Controls.Add(CreateCardPanel(), 0, 1);

            ConfigureStatusLabel();
            contentLayout.Controls.Add(_statusLabel, 0, 2);

            return contentLayout;
        }

        private Control CreateWindowHeader()
        {
            var headerPanel = new WinFormsPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(12, 16, 24)
            };
            headerPanel.MouseDown += HeaderPanel_MouseDown;

            var headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var titleLabel = new PoisonLabel
            {
                Text = "Markdown Converter Pro",
                Font = new Font("Segoe UI Semibold", 11F),
                ForeColor = Color.FromArgb(224, 232, 247),
                AutoSize = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(12, 0, 0, 0)
            };
            titleLabel.MouseDown += HeaderPanel_MouseDown;

            var buttonGroup = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            buttonGroup.MouseDown += HeaderPanel_MouseDown;

            var closeButton = new PoisonButton
            {
                Text = "✕",
                Width = 34,
                Height = 26,
                Padding = new Padding(0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(204, 72, 72),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 6, 8, 6)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => Close();

            var minimizeButton = new PoisonButton
            {
                Text = "—",
                Width = 34,
                Height = 26,
                Padding = new Padding(0),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(48, 56, 70),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(2, 6, 2, 6)
            };
            minimizeButton.FlatAppearance.BorderSize = 0;
            minimizeButton.Click += (s, e) => WindowState = FormWindowState.Minimized;

            buttonGroup.Controls.Add(closeButton);
            buttonGroup.Controls.Add(minimizeButton);

            headerLayout.Controls.Add(titleLabel, 1, 0);
            headerLayout.Controls.Add(buttonGroup, 2, 0);

            headerPanel.Controls.Add(headerLayout);
            return headerPanel;
        }

        private Control CreateHeroPanel()
        {
            var heroLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                RowCount = 2,
                Padding = new Padding(0, 0, 0, 4),
                AutoSize = true
            };
            heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            heroLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var heroTitle = new PoisonLabel
            {
                Text = "Markdown Converter",
                Font = new Font("Segoe UI Semibold", 28F),
                ForeColor = Color.FromArgb(236, 239, 244),
                AutoSize = true
            };

            var heroSubtitle = new PoisonLabel
            {
                Text = "Turn markdown into PDF, Word, or Excel with a single click.",
                Font = new Font("Segoe UI", 12F),
                ForeColor = Color.FromArgb(172, 183, 200),
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0)
            };

            heroLayout.Controls.Add(heroTitle, 0, 0);
            heroLayout.Controls.Add(heroSubtitle, 0, 1);

            var heroPanel = new WinFormsPanel
            {
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 12)
            };
            heroPanel.Controls.Add(heroLayout);
            return heroPanel;
        }

        private WinFormsPanel CreateCardPanel()
        {
            var cardPanel = new WinFormsPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(26),
                BackColor = Color.FromArgb(24, 28, 40),
                Margin = new Padding(0, 12, 0, 12),
                BorderStyle = BorderStyle.None
            };

            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true
            };
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            formLayout.Controls.Add(CreateSectionLabel("Select the Markdown file"), 0, 0);
            formLayout.Controls.Add(CreateFileSelectionLayout(), 0, 1);
            formLayout.Controls.Add(CreateSectionLabel("Choose the target format"), 0, 2);
            formLayout.Controls.Add(CreateFormatLayout(), 0, 3);
            formLayout.Controls.Add(CreateActionPanel(), 0, 4);

            cardPanel.Controls.Add(formLayout);
            return cardPanel;
        }

        private Control CreateSectionLabel(string text)
        {
            return new PoisonLabel
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 10F),
                ForeColor = Color.FromArgb(211, 220, 236),
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(0, 0, 0, 6)
            };
        }

        private TableLayoutPanel CreateFileSelectionLayout()
        {
            var fileLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true
            };
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            fileLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            _filePathTextBox.ReadOnly = true;
            _filePathTextBox.Dock = DockStyle.Fill;
            _filePathTextBox.Height = 44;
            _filePathTextBox.Margin = new Padding(0, 0, 8, 0);
            _filePathTextBox.Font = new Font("Segoe UI", 10F);
            _filePathTextBox.BackColor = Color.FromArgb(18, 22, 33);
            _filePathTextBox.ForeColor = Color.FromArgb(230, 233, 242);
            _filePathTextBox.TextChanged += (s, e) => UpdateConvertEnabled();

            _browseButton.Text = "Browse File";
            _browseButton.Padding = new Padding(12, 5, 12, 5);
            _browseButton.Font = new Font("Segoe UI Semibold", 10F);
            _browseButton.Height = 44;
            _browseButton.Anchor = AnchorStyles.Right;
            _browseButton.Click += (s, e) => BrowseFile();
            _browseButton.BackColor = Color.FromArgb(45, 55, 75);
            _browseButton.ForeColor = Color.White;
            _browseButton.FlatStyle = FlatStyle.Flat;
            _browseButton.FlatAppearance.BorderSize = 0;

            fileLayout.Controls.Add(_filePathTextBox, 0, 0);
            fileLayout.Controls.Add(_browseButton, 1, 0);

            return fileLayout;
        }

        private Control CreateFormatLayout()
        {
            var formatLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 4, 0, 0)
            };

            _formatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _formatComboBox.Width = 320;
            _formatComboBox.Height = 42;
            _formatComboBox.Font = new Font("Segoe UI", 10F);
            _formatComboBox.BackColor = Color.FromArgb(18, 22, 33);
            _formatComboBox.FlatStyle = FlatStyle.Flat;
            _formatComboBox.ForeColor = Color.FromArgb(234, 238, 247);
            _formatComboBox.DisplayMember = nameof(FormatOption.Display);
            _formatComboBox.ValueMember = nameof(FormatOption.Value);
            _formatComboBox.DataSource = _formatOptions;
            if (_formatComboBox.Items.Count > 0)
            {
                _formatComboBox.SelectedIndex = 0;
            }
            _formatComboBox.SelectedIndexChanged += (s, e) => UpdateFormatSelection();

            formatLayout.Controls.Add(_formatComboBox);
            formatLayout.Controls.Add(new PoisonLabel
            {
                Text = "Need multipage documentation? PDF keeps the fidelity.",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(151, 168, 199),
                Margin = new Padding(12, 12, 0, 0)
            });

            return formatLayout;
        }

        private Control CreateActionPanel()
        {
            _convertButton.Text = "Convert File";
            _convertButton.AutoSize = true;
            _convertButton.Padding = new Padding(16, 8, 16, 8);
            _convertButton.Font = new Font("Segoe UI Semibold", 11F);
            _convertButton.Enabled = false;
            _convertButton.ForeColor = Color.White;
            _convertButton.BackColor = Color.FromArgb(36, 176, 223);
            _convertButton.FlatStyle = FlatStyle.Flat;
            _convertButton.FlatAppearance.BorderSize = 0;
            _convertButton.Cursor = Cursors.Hand;
            _convertButton.Click += (s, e) => ConvertFile(_filePathTextBox.Text);

            var actionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Padding = new Padding(0, 16, 0, 0)
            };

            actionPanel.Controls.Add(_convertButton);
            return actionPanel;
        }

        private void ConfigureStatusLabel()
        {
            _statusLabel.AutoSize = false;
            _statusLabel.Height = 48;
            _statusLabel.Dock = DockStyle.Fill;
            _statusLabel.Font = new Font("Segoe UI", 10F);
            _statusLabel.Padding = new Padding(12, 0, 12, 0);
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusLabel.BackColor = Color.FromArgb(20, 25, 34);
            _statusLabel.ForeColor = Color.FromArgb(176, 201, 255);
            _statusLabel.Text = "Select a Markdown file to begin.";
        }

        private void BrowseFile()
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "Markdown Files (*.md;*.markdown)|*.md;*.markdown|All Files (*.*)|*.*",
                Title = "Select Markdown File"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _selectedFilePath = openFileDialog.FileName;
                _filePathTextBox.Text = _selectedFilePath;
                _toolTip.SetToolTip(_filePathTextBox, _selectedFilePath);
                UpdateStatus($"Ready to convert: {Path.GetFileName(_selectedFilePath)}", Color.FromArgb(0, 135, 75));
            }
        }

        private void ConvertFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                UpdateStatus("Error: Please select a valid Markdown file first.", Color.FromArgb(190, 15, 15));
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                UpdateStatus("Converting file... Please wait.", Color.FromArgb(10, 90, 200));

                var markdownText = MarkdownConverterService.ReadMarkdownFile(filePath);
                var outputDir = Path.GetDirectoryName(filePath) ?? Path.GetPathRoot(Path.GetFullPath(filePath)) ?? Environment.CurrentDirectory;
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                string outputPath = _selectedFormat switch
                {
                    "pdf" => Path.Combine(outputDir, $"{fileNameWithoutExt}.pdf"),
                    "docx" => Path.Combine(outputDir, $"{fileNameWithoutExt}.docx"),
                    "xlsx" => Path.Combine(outputDir, $"{fileNameWithoutExt}.xlsx"),
                    _ => throw new NotSupportedException($"Format not supported: {_selectedFormat}")
                };

                switch (_selectedFormat)
                {
                    case "pdf":
                        PdfConverter.ConvertToPdf(markdownText, outputPath);
                        break;
                    case "docx":
                        WordConverter.ConvertToWord(markdownText, outputPath);
                        break;
                    case "xlsx":
                        ExcelConverter.ConvertToExcel(markdownText, outputPath);
                        break;
                }

                Cursor = Cursors.Default;
                UpdateStatus($"Conversion successful. Saved to: {Path.GetFileName(outputPath)}", Color.FromArgb(0, 135, 75));

                var openFolder = MessageBox.Show(
                    "Open folder containing converted file?",
                    "Success",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (openFolder == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                UpdateStatus($"Error: {ex.Message}", Color.FromArgb(190, 15, 15));
                MessageBox.Show($"Conversion failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = color;
        }

        private void HeaderPanel_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
            }
        }

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private void UpdateConvertEnabled()
        {
            _convertButton.Enabled = File.Exists(_filePathTextBox.Text);
        }

        private void UpdateFormatSelection()
        {
            if (_formatComboBox.SelectedItem is FormatOption option)
            {
                _selectedFormat = option.Value;
                _toolTip.SetToolTip(_formatComboBox, option.Display);
            }
        }

        private sealed record FormatOption(string Display, string Value);
    }
}
