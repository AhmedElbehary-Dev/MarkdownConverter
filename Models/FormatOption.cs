namespace MarkdownConverter.Models
{
    public sealed class FormatOption
    {
        public FormatOption(string label, OutputFormat value, string extension)
        {
            Label = label;
            Value = value;
            Extension = extension;
        }

        public string Label { get; }

        public OutputFormat Value { get; }

        public string Extension { get; }
    }
}
