using System;

namespace MarkdownConverter.Models
{
    public class PdfPageImage
    {
        public byte[] BgraPixels { get; set; } = Array.Empty<byte>();
        public int Width { get; set; }
        public int Height { get; set; }
        public int PageNumber { get; set; }
    }
}
