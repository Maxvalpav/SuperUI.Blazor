// SuperUI/Base/Localization/ISuperUILocalizer.cs
using System;
using System.Globalization;

namespace SuperUI.Base.Localization;

/// <summary>
/// Interface for SuperUI localization. Provides localized strings
/// for all built-in components. Supports resource-based and
/// custom localization providers.
/// </summary>
public interface ISuperUILocalizer
{
    /// <summary>Current culture used for localization.</summary>
    CultureInfo CurrentCulture { get; set; }

    /// <summary>Get a localized string by key.</summary>
    string this[string key] { get; }

    /// <summary>Try to get a localized string.</summary>
    bool TryGetString(string key, out string value);

    /// <summary>Get a localized string with format arguments.</summary>
    string Format(string key, params object[] args);

    /// <summary>Event raised when the culture/locale changes.</summary>
    event Action<CultureInfo>? CultureChanged;
}
