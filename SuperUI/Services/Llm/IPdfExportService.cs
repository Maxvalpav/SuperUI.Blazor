using System.Threading.Tasks;
using SuperUI.Components.Llm.Models;

namespace SuperUI.Services.Llm;

public interface IPdfExportService
{
    Task<ExportedFile> ExportAsync(ExtractedData data, ExportOptions options);
}
