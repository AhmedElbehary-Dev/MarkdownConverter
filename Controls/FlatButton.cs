using System.Windows;

namespace MarkdownConverter.Controls
{
    public class FlatButton : System.Windows.Controls.Button
    {
        static FlatButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FlatButton),
                new FrameworkPropertyMetadata(typeof(FlatButton)));
        }
    }
}
