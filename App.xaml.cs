using DinkToPdfAll;
using System.Text;

namespace MarkdownConverter
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            LibraryLoader.Load();
            base.OnStartup(e);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
