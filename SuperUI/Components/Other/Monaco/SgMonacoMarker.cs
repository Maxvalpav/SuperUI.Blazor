using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>Represents a diagnostic marker (error/warning/info/hint) displayed in the Monaco editor gutter and as squiggly underlines.</summary>
public class SgMonacoMarker
{
    /// <summary>1-based line number where the marker appears.</summary>
    public int Line { get; set; }

    /// <summary>1-based column number where the marker starts.</summary>
    public int Column { get; set; }

    /// <summary>Marker tooltip / message text.</summary>
    public string Message { get; set; } = "";

    /// <summary>Severity level that controls the marker's color and icon.</summary>
    public SgMonacoMarkerSeverity Severity { get; set; }
}
