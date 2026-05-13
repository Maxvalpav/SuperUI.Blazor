namespace SuperUI;

/// <summary>
/// Configuration options for SuperUI components. Register via <see cref="SuperUI.ServiceCollectionExtensions.AddSuperUI(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Action{SuperUI.SuperUiOptions})"/>.
/// </summary>
public sealed class SuperUiOptions
{
    /// <summary>
    /// Default lifetime of toast notifications, in milliseconds.
    /// </summary>
    public int DefaultToastDurationMs { get; set; } = 4000;

    /// <summary>
    /// Maximum number of toast notifications visible at the same time.
    /// Older toasts are dismissed when the limit is exceeded.
    /// </summary>
    public int MaxVisibleToasts { get; set; } = 5;

    /// <summary>

}
