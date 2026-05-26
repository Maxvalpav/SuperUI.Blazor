namespace SuperUI.Enums;

/// <summary>Animation style for BackTop/BackBottom appear/disappear transitions.</summary>
public enum SgBackTopAnimation
{
    /// <summary>No animation — instant show/hide.</summary>
    None,
    /// <summary>Scale up on appear, scale down on disappear.</summary>
    Scale,
    /// <summary>Fade in/out.</summary>
    Fade,
    /// <summary>Slide up on appear, slide down on disappear.</summary>
    SlideUp,
    /// <summary>Elastic bounce effect.</summary>
    Bounce
}
