using MarkdownConverter.ViewModels;
using System.Windows;

namespace MarkdownConverter.UI
{
    public partial class MessageDialog : Window
    {
        public MessageDialog(string message, ToastKind kind)
        {
            InitializeComponent();
            MessageText.Text = message;

            if (kind == ToastKind.Success)
            {
                IconGlyph.Text = "\uE73E";
                IconGlyph.Foreground = (System.Windows.Media.Brush)FindResource("BrushSuccess");
            }
            else
            {
                IconGlyph.Text = "\uE711";
                IconGlyph.Foreground = (System.Windows.Media.Brush)FindResource("BrushDanger");
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
