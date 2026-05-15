namespace SuperUI.Enums;

/// <summary>ABC classification used for warehouse slotting priority.</summary>
public enum SgWhAbcClass
{
    /// <summary>No classification set.</summary>
    None = 0,
    /// <summary>Class A — fast-moving / high-value.</summary>
    A = 1,
    /// <summary>Class B — medium-moving.</summary>
    B = 2,
    /// <summary>Class C — slow-moving / low-value.</summary>
    C = 3
}
