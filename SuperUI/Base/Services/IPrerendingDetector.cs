namespace SuperUI.Base.Services;

/// <summary>
/// Сервис определения режима пререндеринга.
/// </summary>
public interface IPrerenderingDetector
{
    /// <summary>true во время статического SSR prerender — JS недоступен.</summary>
    bool IsPrerendering { get; }

    /// <summary>true когда компонент интерактивен (SignalR или WASM).</summary>
    bool IsInteractive { get; }
}

/// <summary>
/// Обратная совместимость: устаревший алиас с опечаткой.
/// </summary>
[Obsolete("Use IPrerenderingDetector (fixed spelling). Will be removed in v2.0.", error: false)]
public interface IPrerendingDetector : IPrerenderingDetector { }
