using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Core;

/// <summary>
/// Keyboard shortcut matching and dispatch. Use the static <see cref="Match"/> for one-off
/// checks (<c>if (SgKeyboardHandler.Match(e, "Ctrl+K")) ...</c>) or create an instance to
/// register a keymap and dispatch a single <see cref="KeyboardEventArgs"/> across many bindings.
/// </summary>
/// <remarks>
/// Shortcut syntax: <c>[Modifier+]*Key</c>, modifiers in any order. Recognised modifiers:
/// <c>Ctrl</c>, <c>Shift</c>, <c>Alt</c>, <c>Meta</c> (alias <c>Cmd</c>). Keys match
/// <see cref="KeyboardEventArgs.Key"/> case-insensitively (so <c>"a"</c> matches <c>"A"</c>),
/// with friendly aliases for non-printables: <c>Esc</c>, <c>Space</c>, <c>Enter</c>, <c>Up</c>,
/// <c>Down</c>, <c>Left</c>, <c>Right</c>, <c>Plus</c>, <c>Minus</c>.
/// </remarks>
public sealed class SgKeyboardHandler
{
    private readonly List<Binding> _bindings = new();

    /// <summary>True if the keyboard event matches the shortcut descriptor.</summary>
    public static bool Match(KeyboardEventArgs e, string shortcut)
    {
        if (e is null || string.IsNullOrWhiteSpace(shortcut)) return false;

        bool needCtrl = false, needShift = false, needAlt = false, needMeta = false;
        string? key = null;

        foreach (var raw in shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "ctrl":
                case "control": needCtrl = true; break;
                case "shift": needShift = true; break;
                case "alt":
                case "option": needAlt = true; break;
                case "meta":
                case "cmd":
                case "command":
                case "win": needMeta = true; break;
                default:
                    key = NormalizeKey(raw);
                    break;
            }
        }

        if (key is null) return false;
        if (needCtrl != e.CtrlKey) return false;
        if (needShift != e.ShiftKey) return false;
        if (needAlt != e.AltKey) return false;
        if (needMeta != e.MetaKey) return false;

        return string.Equals(NormalizeKey(e.Key), key, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Registers a binding. Returns <see langword="this"/> for chaining.</summary>
    public SgKeyboardHandler On(string shortcut, Func<Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _bindings.Add(new Binding(shortcut, handler));
        return this;
    }

    /// <summary>Registers a synchronous binding.</summary>
    public SgKeyboardHandler On(string shortcut, Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return On(shortcut, () => { handler(); return Task.CompletedTask; });
    }

    /// <summary>Removes all bindings matching <paramref name="shortcut"/>.</summary>
    public void Off(string shortcut)
    {
        _bindings.RemoveAll(b => string.Equals(b.Shortcut, shortcut, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Removes all registered bindings.</summary>
    public void Clear() => _bindings.Clear();

    /// <summary>
    /// Dispatches <paramref name="e"/> to the first matching binding. Returns <c>true</c>
    /// when a binding fired so callers can call <c>preventDefault</c>.
    /// </summary>
    public async Task<bool> HandleAsync(KeyboardEventArgs e)
    {
        if (e is null) return false;
        foreach (var binding in _bindings)
        {
            if (Match(e, binding.Shortcut))
            {
                await binding.Handler().ConfigureAwait(false);
                return true;
            }
        }
        return false;
    }

    private static string NormalizeKey(string key) => key.ToLowerInvariant() switch
    {
        "esc" => "escape",
        "space" or "spacebar" or " " => " ",
        "enter" or "return" => "enter",
        "up" => "arrowup",
        "down" => "arrowdown",
        "left" => "arrowleft",
        "right" => "arrowright",
        "plus" => "+",
        "minus" => "-",
        "del" => "delete",
        "ins" => "insert",
        _ => key.ToLowerInvariant()
    };

    private readonly record struct Binding(string Shortcut, Func<Task> Handler);
}
