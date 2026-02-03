using System.Windows;

namespace MarkdownConverter.Controls
{
    public class SegmentedControl : System.Windows.Controls.ListBox
    {
        static SegmentedControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SegmentedControl),
                new FrameworkPropertyMetadata(typeof(SegmentedControl)));
        }

        public SegmentedControl()
        {
            SelectionMode = System.Windows.Controls.SelectionMode.Single;
        }
    }
}
