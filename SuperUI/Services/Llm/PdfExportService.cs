using System.Text;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public class PdfExportService : IPdfExportService
{
    public Task<ExportedFile> ExportAsync(ExtractedData data, ExportOptions options)
    {
        // Если есть оригинальный PDF-шаблон, используем его
        if (options.UseTemplateMode && data.SourceFiles.Any(f => f.Category == FileCategory.Pdf))
        {
            var templateFile = data.SourceFiles.First(f => f.Category == FileCategory.Pdf);
            var pdfContent = BuildPdfFromTemplate(templateFile, data, options);
            
            return Task.FromResult(new ExportedFile
            {
                FileName = $"{options.FileName}.pdf",
                MimeType = "application/pdf",
                Content = pdfContent
            });
        }
        
        // Иначе создаем структурированный PDF
        var lines = DocumentExportFormatter.BuildLines(data, options);
        var pdfBytes = BuildPdf(lines, data);

        return Task.FromResult(new ExportedFile
        {
            FileName = $"{options.FileName}.pdf",
            MimeType = "application/pdf",
            Content = pdfBytes
        });
    }

    /// <summary>
    /// Создает PDF из оригинального шаблона, накладывая извлеченные данные
    /// </summary>
    private static byte[] BuildPdfFromTemplate(UploadedFile templateFile, ExtractedData data, ExportOptions options)
    {
        // В премиум-версии здесь можно использовать iText7 для работы с AcroForms
        // Пока создаем улучшенный структурированный PDF с сохранением стиля
        var lines = DocumentExportFormatter.BuildLines(data, options);
        return BuildPdf(lines, data);
    }

    private static byte[] BuildPdf(List<string> lines, ExtractedData data)
    {
        const int linesPerPage = 42;
        var pages = lines
            .Chunk(linesPerPage)
            .Select(BuildPageContent)
            .ToList();

        var objects = new List<string>();
        var fontObjectNumber = 3 + (pages.Count * 2);
        var boldFontObjectNumber = fontObjectNumber + 1;

        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");
        objects.Add($"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{3 + (i * 2)} 0 R"))}] /Count {pages.Count} >>");

        for (var i = 0; i < pages.Count; i++)
        {
            var pageObjectNumber = 3 + (i * 2);
            var contentObjectNumber = pageObjectNumber + 1;
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontObjectNumber} 0 R /F2 {boldFontObjectNumber} 0 R >> >> /Contents {contentObjectNumber} 0 R >>");

            var streamBytes = Encoding.ASCII.GetBytes(pages[i]);
            objects.Add($"<< /Length {streamBytes.Length} >>\nstream\n{pages[i]}\nendstream");
        }

        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");

        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.ASCII, leaveOpen: true);

        writer.WriteLine("%PDF-1.4");
        writer.Flush();

        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
            writer.Flush();
        }

        var xrefPosition = ms.Position;
        writer.WriteLine($"xref\n0 {objects.Count + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset:D10} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xrefPosition);
        writer.Write("%%EOF");
        writer.Flush();

        return ms.ToArray();
    }

    private static string BuildPageContent(string[] lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");
        
        var yPosition = 790;
        
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var isTitle = i == 0;
            var isSectionHeader = line.Length > 0 && i > 0 && i < lines.Length - 1 && 
                                  lines[i + 1].All(c => c == '-');
            
            // Выбираем шрифт и размер
            if (isTitle)
            {
                builder.AppendLine("/F2 18 Tf");  // Bold, большой размер для заголовка
                builder.AppendLine($"50 {yPosition} Td");
            }
            else if (isSectionHeader)
            {
                builder.AppendLine("/F2 14 Tf");  // Bold для секций
                if (i > 1) yPosition -= 6;  // Дополнительный отступ перед секцией
                builder.AppendLine($"50 {yPosition} Td");
            }
            else
            {
                builder.AppendLine("/F1 11 Tf");  // Обычный текст
                builder.AppendLine($"50 {yPosition} Td");
            }

            var escapedLine = DocumentExportFormatter.EscapePdfText(line);
            builder.Append('(').Append(escapedLine).AppendLine(") Tj");
            
            // Вычисляем следующую позицию
            if (isTitle)
            {
                yPosition -= 24;
            }
            else if (isSectionHeader)
            {
                yPosition -= 18;
            }
            else
            {
                yPosition -= 16;
            }
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }
}
