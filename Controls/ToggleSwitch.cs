using System.Windows;
using System.Windows.Controls.Primitives;

namespace MarkdownConverter.Controls
{
    public class ToggleSwitch : ToggleButton
    {
        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
        }
    }
}
