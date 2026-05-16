using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public class DocumentParserService : IDocumentParserService
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;

    public DocumentParserService(IJSRuntime js)
    {
        _js = js;
    }

    private async Task EnsureModuleLoaded()
    {
        if (_module == null)
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/js/documentExtractor.js");
        }
    }

    public async Task<ParsedDocument> ParseAsync(UploadedFile file)
    {
        await EnsureModuleLoaded();
        
        var result = new ParsedDocument { Metadata = new Dictionary<string, string>() };
        result.Metadata["FileName"] = file.FileName;
        result.Metadata["Size"] = file.Size.ToString();

        try
        {
            if (file.Category == FileCategory.Pdf)
            {
                var parsed = await _module!.InvokeAsync<ParsedDocument>("parsePdfDocument", file.Base64Content);
                result.ExtractedText = parsed.ExtractedText;
                result.Pages = parsed.Pages;
                foreach (var item in parsed.Metadata)
                {
                    result.Metadata[item.Key] = item.Value;
                }
            }
            else if (file.Category == FileCategory.Word)
            {
                var parsed = await _module!.InvokeAsync<ParsedDocument>("extractWordDocument", file.Base64Content);
                result.ExtractedText = parsed.ExtractedText;
                foreach (var item in parsed.Metadata)
                {
                    result.Metadata[item.Key] = item.Value;
                }
            }
            else if (file.Category == FileCategory.Image)
            {
                result.Pages.Add(new PageImage 
                { 
                    PageNumber = 1, 
                    Base64 = file.Base64Content, 
                    MimeType = file.ContentType 
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing document {file.FileName}: {ex.Message}");
        }

        return result;
    }
}
