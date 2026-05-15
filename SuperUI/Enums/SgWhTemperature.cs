namespace SuperUI.Enums;

/// <summary>Temperature regime for a warehouse rack / zone.</summary>
public enum SgWhTemperature
{
    /// <summary>Ambient temperature (15–25 °C).</summary>
    Ambient = 0,
    /// <summary>Chilled / refrigerated (2–8 °C).</summary>
    Chilled = 1,
    /// <summary>Frozen (-25 °C).</summary>
    Frozen = 2,
    /// <summary>Climate-controlled / dry storage.</summary>
    ClimateControlled = 3,
    /// <summary>Cool ambient — stored produce / materials.</summary>
    Cool = 4,
    /// <summary>Cold storage (same as Chilled, alternative name).</summary>
    Cold = 5
}
