using System.Globalization;
using Microsoft.AspNetCore.Components;
using SuperUI.Localization;

namespace SuperUI.Components;

/// <summary>
/// Displays a live countdown timer with configurable units, labels, sizes, and finish behavior.
/// Supports both <see cref="TargetDate"/> (absolute) and <see cref="InitialTimeLeft"/> (relative) modes.
/// </summary>
public partial class SgCountdown : IDisposable
{
    private Timer? _timer;
    private TimeSpan _timeLeft;
    private TimeSpan _initialCaptured;
    private bool _initialized;
    private bool _disposed;

    /// <summary>
    /// Absolute target date/time for the countdown. When set, <see cref="InitialTimeLeft"/> is ignored.
    /// </summary>
    [Parameter] public DateTime? TargetDate { get; set; }

    /// <summary>
    /// Relative duration for the countdown. Used when <see cref="TargetDate"/> is null.
    /// </summary>
    [Parameter] public TimeSpan? InitialTimeLeft { get; set; }

    /// <summary>Show the days unit.</summary>
    [Parameter] public bool ShowDays { get; set; } = true;

    /// <summary>Show the hours unit.</summary>
    [Parameter] public bool ShowHours { get; set; } = true;

    /// <summary>Show the minutes unit.</summary>
    [Parameter] public bool ShowMinutes { get; set; } = true;

    /// <summary>Show the seconds unit.</summary>
    [Parameter] public bool ShowSeconds { get; set; } = true;

    /// <summary>Show unit labels (e.g. "days", "hours").</summary>
    [Parameter] public bool ShowLabels { get; set; } = true;

    /// <summary>Show colon separators between unit blocks.</summary>
    [Parameter] public bool ShowSeparators { get; set; } = true;

    /// <summary>Always show the days block even when zero.</summary>
    [Parameter] public bool AlwaysShowDays { get; set; }

    /// <summary>
    /// Size preset. Valid values: "Small", "Medium", "Large". Default "Medium".
    /// </summary>
    [Parameter] public string Size { get; set; } = "Medium";

    /// <summary>Message displayed when the countdown reaches zero.</summary>
    [Parameter] public string? FinishedMessage { get; set; }

    /// <summary>Additional CSS classes for the root element.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Inline styles for the root element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Fires once when the countdown reaches zero.</summary>
    [Parameter] public EventCallback OnFinish { get; set; }

    /// <summary>Fires on every tick (every second) with the remaining <see cref="TimeSpan"/>.</summary>
    [Parameter] public EventCallback<TimeSpan> OnTick { get; set; }

    /// <summary>If true, the countdown restarts from its initial value after finishing.</summary>
    [Parameter] public bool Loop { get; set; }

    /// <summary>Externally pausing the countdown. Set to true to pause, false to resume.</summary>
    [Parameter] public bool Paused { get; set; }

    /// <summary>Whether to show a progress bar beneath the countdown values.</summary>
    [Parameter] public bool ShowProgress { get; set; }

    /// <summary>Whether to animate digit changes with a flip effect.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <summary>Whether to start the countdown automatically on initialization. Default true.</summary>
    [Parameter] public bool AutoStart { get; set; } = true;

    /// <summary>Catch-all for additional HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

    private Action? _localeChangedHandler;

    /// <summary>True when the countdown has reached zero.</summary>
    public bool IsFinished => _timeLeft <= TimeSpan.Zero;

    /// <summary>Current remaining time.</summary>
    public TimeSpan TimeLeft => _timeLeft;

    /// <summary>Progress ratio (0.0 to 1.0) from start to finish. 1.0 = finished.</summary>
    public double Progress
    {
        get
        {
            if (_initialCaptured <= TimeSpan.Zero) return 0;
            var elapsed = _initialCaptured - _timeLeft;
            return Math.Clamp(elapsed.TotalSeconds / _initialCaptured.TotalSeconds, 0, 1);
        }
    }

    /// <summary>Total duration at start.</summary>
    public TimeSpan InitialDuration => _initialCaptured;

    /// <summary>Formatted countdown string for aria-labels.</summary>
    public string AriaLabel
    {
        get
        {
            if (IsFinished) return "Countdown finished";
            return $"{_timeLeft.Days} days, {_timeLeft.Hours} hours, {_timeLeft.Minutes} minutes, {_timeLeft.Seconds} seconds remaining";
        }
    }

    private string SizeClass => (Size ?? "Medium").ToLowerInvariant() switch
    {
        "small" => "small",
        "large" => "large",
        _ => "medium"
    };

    private string UnitClass(bool visible) => visible && ShowLabels ? "" : "sgc-cd-unit-hidden";
    private string SeparatorClass => ShowLabels ? "sgc-cd-sep-labeled" : "sgc-cd-sep-compact";
    private string FinishedClass => IsFinished ? "finished" : "";

    /// <summary>Returns a display value for a unit segment, formatted with leading zero.</summary>
    public string FormatValue(int value) => value.ToString("D2", CultureInfo.InvariantCulture);

    /// <summary>Starts or resumes the countdown.</summary>
    public void Start()
    {
        if (_disposed) return;
        if (IsFinished && !Loop) return;

        Stop();
        _timer = new Timer(OnTimerTick, null, 0, 1000);
    }

    /// <summary>Stops/pauses the countdown.</summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>Resets to a new target date and starts.</summary>
    public void Reset(DateTime newTarget)
    {
        TargetDate = newTarget;
        InitialTimeLeft = null;
        UpdateTimeLeft();
        _initialCaptured = _timeLeft;
        Stop();
        Start();
    }

    /// <summary>Resets to a new time span and starts.</summary>
    public void Reset(TimeSpan newTime)
    {
        TargetDate = null;
        InitialTimeLeft = newTime;
        _timeLeft = newTime;
        _initialCaptured = _timeLeft;
        Stop();
        Start();
    }

    /// <summary>Restarts from the original initial value.</summary>
    public void Restart()
    {
        if (_initialCaptured <= TimeSpan.Zero) return;
        _timeLeft = _initialCaptured;
        Stop();
        Start();
        InvokeAsync(StateHasChanged);
    }

    protected override void OnInitialized()
    {
        _localeChangedHandler = () => { try { InvokeAsync(StateHasChanged); } catch { } };
        Localizer.OnLocaleChanged += _localeChangedHandler;
        UpdateTimeLeft();
        _initialCaptured = _timeLeft;
        _initialized = true;
        if (AutoStart && !IsFinished)
        {
            Start();
        }
    }

    protected override void OnParametersSet()
    {
        if (!_initialized) return;

        if (Paused && _timer != null)
        {
            Stop();
        }
        else if (!Paused && _timer == null && !IsFinished)
        {
            Start();
        }
    }

    private void UpdateTimeLeft()
    {
        if (TargetDate.HasValue)
        {
            _timeLeft = TargetDate.Value - DateTime.Now;
            if (_timeLeft < TimeSpan.Zero) _timeLeft = TimeSpan.Zero;
        }
        else if (InitialTimeLeft.HasValue)
        {
            _timeLeft = InitialTimeLeft.Value;
        }
    }

    private void OnTimerTick(object? state)
    {
        if (_disposed) return;

        try
        {
            if (TargetDate.HasValue)
            {
                _timeLeft = TargetDate.Value - DateTime.Now;
            }
            else if (_timeLeft > TimeSpan.Zero)
            {
                _timeLeft = _timeLeft.Subtract(TimeSpan.FromSeconds(1));
            }

            if (_timeLeft <= TimeSpan.Zero)
            {
                _timeLeft = TimeSpan.Zero;
                Stop();

                if (Loop)
                {
                    Restart();
                    return;
                }

                InvokeAsync(async () =>
                {
                    await OnFinish.InvokeAsync();
                    StateHasChanged();
                });
                return;
            }

            InvokeAsync(async () =>
            {
                await OnTick.InvokeAsync(_timeLeft);
                StateHasChanged();
            });
        }
        catch (ObjectDisposedException)
        {
            // Component was disposed during callback
        }
        catch (Exception)
        {
            // Suppress unhandled timer exceptions
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_localeChangedHandler is not null)
            Localizer.OnLocaleChanged -= _localeChangedHandler;
        Stop();
    }
}
