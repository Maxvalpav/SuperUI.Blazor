namespace SuperUI.Components;

// ── Scene type ────────────────────────────────────────────────────────────────

/// <summary>Built-in scene presets for <see cref="SgThree"/>.</summary>
public enum SgThreeScene
{
    /// <summary>Empty scene — user provides scene via JS callback.</summary>
    Custom,
    /// <summary>Interactive warehouse floor-plan with rack cells.</summary>
    Warehouse,
    /// <summary>Factory floor with machines and conveyor belt.</summary>
    Factory,
    /// <summary>Pipeline / process flow diagram in 3D.</summary>
    Pipeline,
    /// <summary>3-D bar chart built from box geometries.</summary>
    BarChart3D,
    /// <summary>Rotating 3-D cube with Phong shading.</summary>
    RotatingCube,
    /// <summary>Particle field / star field.</summary>
    ParticleField,
}

// ── Camera ────────────────────────────────────────────────────────────────────

/// <summary>Camera projection type.</summary>
public enum SgThreeCameraType
{
    Perspective,
    Orthographic,
}

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Configuration options for <see cref="SgThree"/>.</summary>
public class SgThreeOptions
{
    /// <summary>Background colour (CSS colour string). Default <c>"#1a1a2e"</c>.</summary>
    public string BackgroundColor { get; set; } = "#1a1a2e";

    /// <summary>Enable orbit controls (mouse drag to rotate, scroll to zoom). Default <c>true</c>.</summary>
    public bool OrbitControls { get; set; } = true;

    /// <summary>Enable auto-rotation of the camera. Default <c>false</c>.</summary>
    public bool AutoRotate { get; set; } = false;

    /// <summary>Auto-rotation speed (degrees/second). Default <c>1.0</c>.</summary>
    public double AutoRotateSpeed { get; set; } = 1.0;

    /// <summary>Show axes helper (RGB = XYZ). Default <c>false</c>.</summary>
    public bool ShowAxes { get; set; } = false;

    /// <summary>Show grid helper on the XZ plane. Default <c>false</c>.</summary>
    public bool ShowGrid { get; set; } = false;

    /// <summary>Enable shadow casting. Default <c>false</c>.</summary>
    public bool Shadows { get; set; } = false;

    /// <summary>Ambient light intensity (0–2). Default <c>0.4</c>.</summary>
    public double AmbientIntensity { get; set; } = 0.4;

    /// <summary>Directional light intensity (0–3). Default <c>1.0</c>.</summary>
    public double DirectionalIntensity { get; set; } = 1.0;

    /// <summary>Camera type. Default <see cref="SgThreeCameraType.Perspective"/>.</summary>
    public SgThreeCameraType CameraType { get; set; } = SgThreeCameraType.Perspective;

    /// <summary>Camera field of view in degrees (perspective only). Default <c>60</c>.</summary>
    public double Fov { get; set; } = 60;

    /// <summary>Initial camera position [x, y, z]. Default <c>[5, 5, 5]</c>.</summary>
    public double[] CameraPosition { get; set; } = [5, 5, 5];

    /// <summary>Pixel device ratio cap. Default <c>2</c>.</summary>
    public double MaxPixelRatio { get; set; } = 2;

    /// <summary>Enable anti-aliasing. Default <c>true</c>.</summary>
    public bool Antialias { get; set; } = true;

    /// <summary>Tone mapping exposure. Default <c>1.0</c>.</summary>
    public double Exposure { get; set; } = 1.0;
}

// ── Warehouse data ────────────────────────────────────────────────────────────

/// <summary>Status of a warehouse rack cell.</summary>
public enum SgWarehouseCellStatus
{
    Empty,
    Occupied,
    Reserved,
    Blocked,
}

/// <summary>A single rack cell in the warehouse floor-plan.</summary>
public class SgWarehouseCell
{
    /// <summary>Unique cell identifier (e.g. "A-01-03").</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Rack row label (e.g. "A", "B").</summary>
    public string Row { get; set; } = string.Empty;

    /// <summary>Column index within the rack (1-based).</summary>
    public int Column { get; set; }

    /// <summary>Shelf level (1 = floor level).</summary>
    public int Level { get; set; } = 1;

    /// <summary>Cell occupancy status.</summary>
    public SgWarehouseCellStatus Status { get; set; } = SgWarehouseCellStatus.Empty;

    /// <summary>Optional label shown on the cell.</summary>
    public string? Label { get; set; }

    /// <summary>Optional tooltip / detail text.</summary>
    public string? Detail { get; set; }

    /// <summary>Quantity of items stored in the cell.</summary>
    public int? Quantity { get; set; }

    /// <summary>Weight in kg.</summary>
    public double? WeightKg { get; set; }

    /// <summary>Last updated timestamp (ISO string).</summary>
    public string? UpdatedAt { get; set; }
}

/// <summary>Warehouse layout configuration.</summary>
public class SgWarehouseLayout
{
    /// <summary>Rack rows (e.g. ["A","B","C","D"]).</summary>
    public List<string> Rows { get; set; } = new();

    /// <summary>Number of columns per rack.</summary>
    public int Columns { get; set; } = 10;

    /// <summary>Number of shelf levels per rack.</summary>
    public int Levels { get; set; } = 3;

    /// <summary>Cell data. Cells not listed are treated as Empty.</summary>
    public List<SgWarehouseCell> Cells { get; set; } = new();
}

// ── Bar chart 3D data ─────────────────────────────────────────────────────────

/// <summary>A single bar in a 3-D bar chart.</summary>
public class SgThreeBarItem
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? Group { get; set; }
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Arguments passed to <see cref="SgThree.OnObjectClick"/>.</summary>
public class SgThreeClickEventArgs
{
    /// <summary>Name / id of the clicked 3-D object.</summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>Optional data payload attached to the object.</summary>
    public string? Data { get; set; }

    /// <summary>World-space X coordinate of the click intersection.</summary>
    public double X { get; set; }

    /// <summary>World-space Y coordinate of the click intersection.</summary>
    public double Y { get; set; }

    /// <summary>World-space Z coordinate of the click intersection.</summary>
    public double Z { get; set; }
}
