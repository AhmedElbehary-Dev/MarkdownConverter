using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Docnet.Core;
using Docnet.Core.Models;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using MarkdownConverter.Models;

namespace MarkdownConverter.Services
{
    public class PdfEditorService
    {
        static PdfEditorService()
        {
            // PdfSharp requires this for .NET Core / .NET 5+ to handle older encodings
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        public int GetPageCount(string filePath)
        {
            using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(1080, 1920));
            return docReader.GetPageCount();
        }

        public PdfPageImage GetPageThumbnail(string filePath, int pageIndex)
        {
            // Use Docnet.Core to read the page and generate a thumbnail
            // Use a reasonable dimension to constrain memory for thumbnails
            using var docReader = DocLib.Instance.GetDocReader(filePath, new PageDimensions(800, 800));
            using var pageReader = docReader.GetPageReader(pageIndex);
            
            var width = pageReader.GetPageWidth();
            var height = pageReader.GetPageHeight();
            var rawBytes = pageReader.GetImage(); // Returns BGRA32 byte array

            return new PdfPageImage
            {
                PageNumber = pageIndex + 1,
                BgraPixels = rawBytes,
                Width = width,
                Height = height
            };
        }

        public string DeletePagesAndExport(string inputFilePath, IEnumerable<int> pagesToDelete1Based)
        {
            var deleteSet = new HashSet<int>(pagesToDelete1Based);
            
            if (deleteSet.Count == 0)
            {
                throw new ArgumentException("No pages selected for deletion.");
            }

            var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
            var ext = Path.GetExtension(inputFilePath);

            var deletedPagesStr = string.Join("_", deleteSet.OrderBy(p => p));
            if (deletedPagesStr.Length > 50) 
            {
                // Truncate to avoid too long file names
                deletedPagesStr = deletedPagesStr.Substring(0, 50) + "_etc";
            }

            var newFileName = $"{fileNameWithoutExt}_deleted_{deletedPagesStr}{ext}";
            var outputFilePath = Path.Join(directory, newFileName);

            using (var inputDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import))
            using (var outputDocument = new PdfDocument())
            {
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    int pageNumber1Based = i + 1;
                    if (!deleteSet.Contains(pageNumber1Based))
                    {
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }
                }
                
                outputDocument.Save(outputFilePath);
            }

            return outputFilePath;
        }

        public string ExtractPagesAndExport(string inputFilePath, IEnumerable<int> selectedPages1Based)
        {
            var selectSet = new HashSet<int>(selectedPages1Based);
            if (selectSet.Count == 0) throw new ArgumentException("No pages selected for extraction.");

            var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
            var ext = Path.GetExtension(inputFilePath);

            var pagesStr = string.Join("_", selectSet.OrderBy(p => p));
            if (pagesStr.Length > 50) pagesStr = pagesStr.Substring(0, 50) + "_etc";

            var outputFilePath = Path.Join(directory, $"{fileNameWithoutExt}_extracted_{pagesStr}{ext}");

            using (var inputDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import))
            using (var outputDocument = new PdfDocument())
            {
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    if (selectSet.Contains(i + 1))
                    {
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }
                }
                outputDocument.Save(outputFilePath);
            }

            return outputFilePath;
        }

        public string ReverseOrderAndExport(string inputFilePath)
        {
            var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
            var ext = Path.GetExtension(inputFilePath);
            var outputFilePath = Path.Join(directory, $"{fileNameWithoutExt}_reversed{ext}");

            using (var inputDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import))
            using (var outputDocument = new PdfDocument())
            {
                for (int i = inputDocument.PageCount - 1; i >= 0; i--)
                {
                    outputDocument.AddPage(inputDocument.Pages[i]);
                }
                outputDocument.Save(outputFilePath);
            }

            return outputFilePath;
        }

        public string ReplacePageAndExport(string inputFilePath, int targetPage1Based, int replacementPage1Based)
        {
            var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
            var ext = Path.GetExtension(inputFilePath);
            var outputFilePath = Path.Join(directory, $"{fileNameWithoutExt}_replaced_p{targetPage1Based}_with_p{replacementPage1Based}{ext}");

            using (var inputDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import))
            using (var outputDocument = new PdfDocument())
            {
                if (targetPage1Based < 1 || targetPage1Based > inputDocument.PageCount) throw new ArgumentException("Invalid target page.");
                if (replacementPage1Based < 1 || replacementPage1Based > inputDocument.PageCount) throw new ArgumentException("Invalid replacement page.");
                if (targetPage1Based == replacementPage1Based) throw new ArgumentException("Target and replacement pages must be different.");

                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    int currentPage1Based = i + 1;
                    if (currentPage1Based == targetPage1Based)
                    {
                        outputDocument.AddPage(inputDocument.Pages[replacementPage1Based - 1]);
                    }
                    else
                    {
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }
                }
                outputDocument.Save(outputFilePath);
            }

            return outputFilePath;
        }

        public string DuplicatePagesAndExport(string inputFilePath, IEnumerable<int> selectedPages1Based)
        {
            var duplicateSet = new HashSet<int>(selectedPages1Based);
            if (duplicateSet.Count == 0) throw new ArgumentException("No pages selected for duplication.");

            var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
            var ext = Path.GetExtension(inputFilePath);

            var pagesStr = string.Join("_", duplicateSet.OrderBy(p => p));
            if (pagesStr.Length > 50) pagesStr = pagesStr.Substring(0, 50) + "_etc";

            var outputFilePath = Path.Join(directory, $"{fileNameWithoutExt}_duplicated_{pagesStr}{ext}");

            using (var inputDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import))
            using (var outputDocument = new PdfDocument())
            {
                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    int pageNumber1Based = i + 1;
                    outputDocument.AddPage(inputDocument.Pages[i]);
                    if (duplicateSet.Contains(pageNumber1Based))
                    {
                        outputDocument.AddPage(inputDocument.Pages[i]);
                    }
                }
                outputDocument.Save(outputFilePath);
            }

            return outputFilePath;
        }

        public string InsertBlankPageAndExport(string inputFilePath, int insertAtIndex1Based)
        {
            var directory = Path.GetDirectoryName(inputFilePath) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputFilePath);
            var ext = Path.GetExtension(inputFilePath);
            var outputFilePath = Path.Join(directory, $"{fileNameWithoutExt}_blank_at_p{insertAtIndex1Based}{ext}");

            using (var inputDocument = PdfReader.Open(inputFilePath, PdfDocumentOpenMode.Import))
            using (var outputDocument = new PdfDocument())
            {
                if (insertAtIndex1Based < 1 || insertAtIndex1Based > inputDocument.PageCount + 1) 
                    throw new ArgumentException("Invalid insertion index.");

                for (int i = 0; i < inputDocument.PageCount; i++)
                {
                    int currentPage1Based = i + 1;
                    if (currentPage1Based == insertAtIndex1Based)
                    {
                        outputDocument.AddPage();
                    }
                    outputDocument.AddPage(inputDocument.Pages[i]);
                }
                
                if (insertAtIndex1Based == inputDocument.PageCount + 1)
                {
                    outputDocument.AddPage();
                }

                outputDocument.Save(outputFilePath);
            }

            return outputFilePath;
        }
    }
}
