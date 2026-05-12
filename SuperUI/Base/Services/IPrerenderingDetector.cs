// SuperUI/Base/Services/IPrerenderingDetector.cs

namespace SuperUI.Base.Services;

public interface IPrerenderingDetector
{
    bool IsPrerendering { get; }
    bool IsInteractive { get; }
}

[Obsolete("Use IPrerenderingDetector (correct spelling). IPrerendingDetector will be removed in SuperUI v2.0.", error: false)]
public interface IPrerendingDetector : IPrerenderingDetector { }
