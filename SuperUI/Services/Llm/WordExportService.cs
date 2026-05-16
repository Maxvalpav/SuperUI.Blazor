using System.IO.Compression;
using System.Text;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public class WordExportService : IWordExportService
{
    public Task<ExportedFile> ExportAsync(ExtractedData data, ExportOptions options)
    {
        // Если есть оригинальный Word-шаблон, используем его
        if (options.UseTemplateMode && data.SourceFiles.Any(f => f.Category == FileCategory.Word))
        {
            var templateFile = data.SourceFiles.First(f => f.Category == FileCategory.Word);
            var docxContent = BuildDocxFromTemplate(templateFile, data, options);
            
            return Task.FromResult(new ExportedFile
            {
                FileName = $"{options.FileName}.docx",
                MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                Content = docxContent
            });
        }
        
        // Иначе создаем структурированный Word
        var lines = DocumentExportFormatter.BuildLines(data, options);
        var docxBytes = BuildDocx(lines, data);

        return Task.FromResult(new ExportedFile
        {
            FileName = $"{options.FileName}.docx",
            MimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            Content = docxBytes
        });
    }

    /// <summary>
    /// Создает Word из оригинального шаблона, заменяя плейсхолдеры
    /// </summary>
    private static byte[] BuildDocxFromTemplate(UploadedFile templateFile, ExtractedData data, ExportOptions options)
    {
        // В премиум-версии здесь можно использовать DocumentFormat.OpenXml для замены Content Controls
        // Пока создаем улучшенный структурированный Word с сохранением стиля
        var lines = DocumentExportFormatter.BuildLines(data, options);
        return BuildDocx(lines, data);
    }

    private static byte[] BuildDocx(List<string> lines, ExtractedData data)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            CreateEntry(archive, "[Content_Types].xml", ContentTypesXml);
            CreateEntry(archive, "_rels/.rels", RootRelsXml);
            CreateEntry(archive, "word/_rels/document.xml.rels", WordDocumentRelsXml);
            CreateEntry(archive, "word/styles.xml", StylesXml);
            CreateEntry(archive, "word/document.xml", BuildDocumentXml(lines, data));
        }

        return memoryStream.ToArray();
    }

    private static void CreateEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string BuildDocumentXml(List<string> lines, ExtractedData data)
    {
        var body = new StringBuilder();
        
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            
            // Определяем стиль на основе содержимого
            string style;
            if (i == 0)
            {
                style = "Title";
            }
            else if (line.Length > 0 && i < lines.Count - 1 && lines[i + 1].All(c => c == '-'))
            {
                style = "Heading1";
            }
            else if (line.All(c => c == '-'))
            {
                continue; // Пропускаем линии-разделители
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                // Пустая строка для отступа
                body.Append(@"<w:p><w:pPr><w:spacing w:after=""200""/></w:pPr></w:p>");
                continue;
            }
            else if (line.Contains(':'))
            {
                // Поле с меткой и значением
                var parts = line.Split(':', 2);
                var label = DocumentExportFormatter.EscapeXml(parts[0].Trim());
                var value = parts.Length > 1 ? DocumentExportFormatter.EscapeXml(parts[1].Trim()) : "";
                
                body.Append($@"<w:p><w:pPr><w:pStyle w:val=""Normal""/></w:pPr>");
                body.Append($@"<w:r><w:rPr><w:b/></w:rPr><w:t xml:space=""preserve"">{label}: </w:t></w:r>");
                body.Append($@"<w:r><w:t xml:space=""preserve"">{value}</w:t></w:r>");
                body.Append(@"</w:p>");
                continue;
            }
            else
            {
                style = "Normal";
            }
            
            var escaped = DocumentExportFormatter.EscapeXml(line);
            body.Append($@"<w:p><w:pPr><w:pStyle w:val=""{style}""/></w:pPr><w:r><w:t xml:space=""preserve"">{escaped}</w:t></w:r></w:p>");
        }

        return $"""
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:wpc="http://schemas.microsoft.com/office/word/2010/wordprocessingCanvas"
            xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
            xmlns:o="urn:schemas-microsoft-com:office:office"
            xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"
            xmlns:m="http://schemas.openxmlformats.org/officeDocument/2006/math"
            xmlns:v="urn:schemas-microsoft-com:vml"
            xmlns:wp14="http://schemas.microsoft.com/office/word/2010/wordprocessingDrawing"
            xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
            xmlns:w10="urn:schemas-microsoft-com:office:word"
            xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            xmlns:w14="http://schemas.microsoft.com/office/word/2010/wordml"
            xmlns:wpg="http://schemas.microsoft.com/office/word/2010/wordprocessingGroup"
            xmlns:wpi="http://schemas.microsoft.com/office/word/2010/wordprocessingInk"
            xmlns:wne="http://schemas.microsoft.com/office/word/2006/wordml"
            xmlns:wps="http://schemas.microsoft.com/office/word/2010/wordprocessingShape"
            mc:Ignorable="w14 wp14">
  <w:body>
    {body}
    <w:sectPr>
      <w:pgSz w:w="11906" w:h="16838"/>
      <w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/>
    </w:sectPr>
  </w:body>
</w:document>
""";
    }

    private const string ContentTypesXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
</Types>
""";

    private const string RootRelsXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>
""";

    private const string WordDocumentRelsXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>
""";

    private const string StylesXml = """
<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:default="1" w:styleId="Normal">
    <w:name w:val="Normal"/>
    <w:qFormat/>
    <w:rPr>
      <w:sz w:val="22"/>
    </w:rPr>
    <w:pPr>
      <w:spacing w:after="120" w:line="276" w:lineRule="auto"/>
    </w:pPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Title">
    <w:name w:val="Title"/>
    <w:basedOn w:val="Normal"/>
    <w:qFormat/>
    <w:rPr>
      <w:b/>
      <w:sz w:val="36"/>
      <w:color w:val="1F4E78"/>
    </w:rPr>
    <w:pPr>
      <w:spacing w:before="240" w:after="240"/>
    </w:pPr>
  </w:style>
  <w:style w:type="paragraph" w:styleId="Heading1">
    <w:name w:val="Heading 1"/>
    <w:basedOn w:val="Normal"/>
    <w:qFormat/>
    <w:rPr>
      <w:b/>
      <w:sz w:val="28"/>
      <w:color w:val="2E75B5"/>
    </w:rPr>
    <w:pPr>
      <w:spacing w:before="240" w:after="120"/>
    </w:pPr>
  </w:style>
</w:styles>
""";
}
