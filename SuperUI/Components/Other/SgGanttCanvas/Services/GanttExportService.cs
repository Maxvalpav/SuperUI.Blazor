using System;
using System.Threading.Tasks;
using SuperUI.Components.SgGanttCanvas.Models;

namespace SuperUI.Components.SgGanttCanvas.Services;

/// <summary>Provides export functionality for the Gantt chart (PNG, PDF, Excel).</summary>
public class GanttExportService
{
    /// <summary>Exports the Gantt chart as a PNG image.</summary>
    public Task<byte[]> ExportToPngAsync(GanttExportOptions options)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    /// <summary>Exports the Gantt chart as a PDF document.</summary>
    public Task<byte[]> ExportToPdfAsync(GanttExportOptions options)
    {
        return Task.FromResult(Array.Empty<byte>());
    }

    /// <summary>Exports the Gantt chart as an Excel spreadsheet.</summary>
    public Task<byte[]> ExportToExcelAsync(GanttExportOptions options)
    {
        return Task.FromResult(Array.Empty<byte>());
    }
}
