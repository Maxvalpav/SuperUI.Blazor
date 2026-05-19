namespace SuperUI.Enums;

/// <summary>Built-in scene presets for <see cref="Components.SgThree"/>.</summary>
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
