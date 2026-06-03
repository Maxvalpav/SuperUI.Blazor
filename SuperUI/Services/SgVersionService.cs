using System.Reflection;
using System.Text.RegularExpressions;

namespace SuperUI.Services;

public class SgVersionService
{
    private string _version;

    public SgVersionService()
    {
        var attr = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        _version = attr?.InformationalVersion ?? "0.0.0";
    }

    public string CurrentVersion => _version;

    public string NextPatch()
    {
        var (major, minor, patch, pre) = Parse(_version);
        return $"{major}.{minor}.{patch + 1}";
    }

    public string NextMinor()
    {
        var (major, minor, _, _) = Parse(_version);
        return $"{major}.{minor + 1}.0";
    }

    public string NextMajor()
    {
        var (major, _, _, _) = Parse(_version);
        return $"{major + 1}.0.0";
    }

    public string NextPreRelease(string tag = "alpha")
    {
        var (major, minor, patch, pre) = Parse(_version);
        if (pre is not null)
        {
            var match = Regex.Match(pre, @"^(.+?)\.?(\d+)$");
            if (match.Success)
            {
                var prefix = match.Groups[1].Value;
                var num = int.Parse(match.Groups[2].Value) + 1;
                return $"{major}.{minor}.{patch}-{prefix}.{num}";
            }
            return $"{major}.{minor}.{patch}-{tag}.1";
        }
        return $"{major}.{minor}.{patch}-{tag}.1";
    }

    public string GitTagFor(string version) => $"v{version}";

    public string ReleaseBranchFor(string version) => $"release/v{version}";

    public string GitFlowCommands(string newVersion)
    {
        var tag = GitTagFor(newVersion);
        var branch = ReleaseBranchFor(newVersion);
        return $$"""
# GitFlow release: {{newVersion}}
git checkout -b {{branch}} main
# Update Version in SuperUI.csproj to {{newVersion}}
dotnet build
git add SuperUI/SuperUI.csproj
git commit -m "chore: bump version to {{newVersion}}"
git tag -a {{tag}} -m "Release {{newVersion}}"
git push origin {{branch}}
git push origin {{tag}}
git checkout main
git merge {{branch}}
git push origin main
""";
    }

    private static (int major, int minor, int patch, string? pre) Parse(string version)
    {
        var match = Regex.Match(version, @"^(\d+)\.(\d+)\.(\d+)(?:-(.+))?$");
        if (!match.Success) return (0, 0, 0, null);
        return (
            int.Parse(match.Groups[1].Value),
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value),
            match.Groups[4].Success ? match.Groups[4].Value : null
        );
    }
}
