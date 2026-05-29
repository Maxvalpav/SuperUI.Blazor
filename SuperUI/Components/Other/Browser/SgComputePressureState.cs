namespace SuperUI.Components;

/// <summary>Enumeration of compute pressure states from the Pressure Observer API.</summary>
public enum SgComputePressureState
{
    /// <summary>System is operating normally.</summary>
    Nominal,
    /// <summary>System is under moderate load.</summary>
    Fair,
    /// <summary>System is under heavy load.</summary>
    Serious,
    /// <summary>System is under critical load.</summary>
    Critical
}
