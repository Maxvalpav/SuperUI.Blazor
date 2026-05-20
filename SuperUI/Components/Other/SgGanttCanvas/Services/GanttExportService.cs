using System;
using System.Threading.Tasks;
using SuperUI.Components.SgGanttCanvas.Models;

namespace SuperUI.Components.SgGanttCanvas.Services;

public class GanttExportService
{
    public Task<byte[]> ExportToPngAsync(GanttExportOptions options)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    public Task<byte[]> ExportToPdfAsync(GanttExportOptions options)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    public Task<byte[]> ExportToExcelAsync(GanttExportOptions options)
    {
        return Task.FromResult(Array.Empty<byte>());
    }
}
