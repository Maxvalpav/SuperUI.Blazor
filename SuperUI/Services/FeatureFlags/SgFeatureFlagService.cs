using System.Collections.Generic;

namespace SuperUI.Services.FeatureFlags;

public class SgFeatureFlagService
{
    private Dictionary<string, bool> _flags = new();

    public void SetFlag(string name, bool enabled) => _flags[name] = enabled;
    public bool IsEnabled(string name) => _flags.TryGetValue(name, out var enabled) && enabled;
    public Dictionary<string, bool> GetAll() => new(_flags);
}
