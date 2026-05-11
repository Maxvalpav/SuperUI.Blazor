// SuperUI/Base/Diagnostics/SgAccessibilityAudit.cs
#if DEBUG

using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// В DEBUG-режиме проверяет ARIA атрибуты компонента и логирует предупреждения.
/// Запускается в OnAfterRenderAsync(firstRender=true).
/// </summary>
public static class SgAccessibilityAudit
{
    public static void Audit(
        string componentId,
        IReadOnlyDictionary<string, object> attrs,
        ILogger logger)
    {
        var issues = new List<string>();

        // Интерактивные элементы должны иметь role или быть нативными
        if (!attrs.ContainsKey("role") && !attrs.ContainsKey("aria-label") &&
            !attrs.ContainsKey("aria-labelledby"))
            issues.Add("Missing accessible name (aria-label or aria-labelledby)");

        // Dialog должен иметь aria-labelledby
        if (attrs.TryGetValue("role", out var role) && role?.ToString() == "dialog" &&
            !attrs.ContainsKey("aria-labelledby"))
            issues.Add("dialog role requires aria-labelledby");

        // aria-required без role=textbox/checkbox — подозрительно
        if (attrs.ContainsKey("aria-required") && !attrs.ContainsKey("role"))
            issues.Add("aria-required without explicit role");

        foreach (var issue in issues)
            logger.LogWarning("[A11Y] [{ComponentId}] {Issue}", componentId, issue);
    }
}
#endif
