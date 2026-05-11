namespace SuperUI.Services;

/// <summary>
/// Сервис определения режима пререндеринга.
/// </summary>
public interface IPrerendingDetector
{
    bool IsPrerendering { get; }
    bool IsInteractive { get; }
}
