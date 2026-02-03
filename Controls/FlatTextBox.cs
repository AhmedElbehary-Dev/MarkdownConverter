using System.Windows;

namespace MarkdownConverter.Controls
{
    public class FlatTextBox : System.Windows.Controls.TextBox
    {
        static FlatTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FlatTextBox),
                new FrameworkPropertyMetadata(typeof(FlatTextBox)));
        }
    }
}
