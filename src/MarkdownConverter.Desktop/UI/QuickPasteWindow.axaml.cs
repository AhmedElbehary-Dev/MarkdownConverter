using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MarkdownConverter.ViewModels;

namespace MarkdownConverter.Desktop.UI
{
    public partial class QuickPasteWindow : Window
    {
        public QuickPasteWindow()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public QuickPasteWindow(QuickPasteViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
    }
}
