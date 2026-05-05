namespace SuperUI.Components;

/// <summary>
/// Source URLs for the Three.js bundle and optional add-ons.
/// Override to ship local copies or pin a specific version.
/// </summary>
/// <remarks>
/// Uses Three.js r134 — the last release that ships a UMD bundle exposing
/// <c>window.THREE</c> globally. Newer releases (r150+) are ES-module only.
/// </remarks>
/// <example>
/// Use local files:
/// <code>
/// new SgThreeSources
/// {
///     ThreeScript   = "/lib/three/three.min.js",
///     OrbitControls = "/lib/three/OrbitControls.js"
/// }
/// </code>
/// </example>
public sealed class SgThreeSources
{
    /// <summary>
    /// Three.js UMD bundle (r134 — last version with global <c>window.THREE</c>).
    /// Set to <c>null</c> if you load Three.js yourself via index.html.
    /// </summary>
    public string? ThreeScript { get; set; } =
        "https://cdnjs.cloudflare.com/ajax/libs/three.js/r134/three.min.js";

    /// <summary>
    /// OrbitControls add-on — enables mouse/touch camera orbit, zoom and pan.
    /// Set to <c>null</c> to disable orbit controls.
    /// </summary>
    public string? OrbitControls { get; set; } =
        "https://unpkg.com/three@0.134.0/examples/js/controls/OrbitControls.js";

    /// <summary>
    /// GLTFLoader add-on — enables loading .gltf / .glb 3D model files.
    /// Set to <c>null</c> if you don't need GLTF loading.
    /// </summary>
    public string? GltfLoader { get; set; } =
        "https://unpkg.com/three@0.134.0/examples/js/loaders/GLTFLoader.js";
}
