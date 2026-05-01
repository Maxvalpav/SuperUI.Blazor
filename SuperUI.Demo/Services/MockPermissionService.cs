using SuperUI.Components;

namespace SuperUI.Demo.Services;

/// <summary>
/// Mock implementation of IPermissionService for demo purposes.
/// Simulates a user with some permissions and roles.
/// </summary>
public class MockPermissionService : IPermissionService
{
    private readonly HashSet<string> _userPermissions = new()
    {
        "CanView",
        "CanEdit",
        "CanCreate"
    };

    private readonly HashSet<string> _userRoles = new()
    {
        "User",
        "Editor"
    };

    public Task<bool> HasPermissionAsync(string permission)
    {
        return Task.FromResult(_userPermissions.Contains(permission));
    }

    public Task<bool> IsInRoleAsync(string role)
    {
        return Task.FromResult(_userRoles.Contains(role));
    }

    public Task<IEnumerable<string>> GetUserPermissionsAsync()
    {
        return Task.FromResult<IEnumerable<string>>(_userPermissions);
    }

    public Task<IEnumerable<string>> GetUserRolesAsync()
    {
        return Task.FromResult<IEnumerable<string>>(_userRoles);
    }
}
