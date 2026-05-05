namespace SuperUI.Components;

/// <summary>
/// Source URLs for the bpmn-js bundle and stylesheets. Override to ship local
/// copies (e.g. <c>/lib/bpmn/bpmn-modeler.development.js</c>) or pin a version.
/// </summary>
public sealed class SgBpmnSources
{
    /// <summary>UMD bundle for the modeler.</summary>
    public string ModelerScript { get; set; } =
        "https://unpkg.com/bpmn-js@17.11.1/dist/bpmn-modeler.development.js";

    /// <summary>UMD bundle for the read-only viewer.</summary>
    public string ViewerScript { get; set; } =
        "https://unpkg.com/bpmn-js@17.11.1/dist/bpmn-viewer.development.js";

    /// <summary>UMD bundle for the navigated viewer (zoom/pan).</summary>
    public string NavigatedViewerScript { get; set; } =
        "https://unpkg.com/bpmn-js@17.11.1/dist/bpmn-navigated-viewer.development.js";

    /// <summary>diagram-js base stylesheet.</summary>
    public string DiagramCss { get; set; } =
        "https://unpkg.com/bpmn-js@17.11.1/dist/assets/diagram-js.css";

    /// <summary>BPMN element font + visuals stylesheet.</summary>
    public string BpmnFontCss { get; set; } =
        "https://unpkg.com/bpmn-js@17.11.1/dist/assets/bpmn-js.css";

    /// <summary>Embedded BPMN icon font stylesheet.</summary>
    public string BpmnEmbeddedCss { get; set; } =
        "https://unpkg.com/bpmn-js@17.11.1/dist/assets/bpmn-font/css/bpmn-embedded.css";
}
