// SuperUI/Base/Diagnostics/ISgDiagnosticsCollector.cs
using System;
using System.Collections.Generic;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Interface for collecting diagnostics across all SuperUI components.
/// Registered as scoped service in DI.
/// </summary>
public interface ISgDiagnosticsCollector
{
    IReadOnlyDictionary<string, ComponentDiagnosticEntry> Entries { get; }

    int TotalRenderCount { get; }

    TimeSpan TotalRenderTime { get; }

    int ErrorCount { get; }

    void RecordRender(string componentId, long elapsedTicks);

    void RecordParameterChange(string componentId, string parameterName);

    void RecordError(string componentId, Exception exception);

    IReadOnlyCollection<ComponentErrorRecord> GetErrors();

    string GetSummary();

    void Reset();
}
