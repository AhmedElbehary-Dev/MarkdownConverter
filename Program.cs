namespace MarkdownConverter;

using System.Text;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Enable legacy code page encodings required by some PDF back-ends (e.g., Windows-1252).
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}
