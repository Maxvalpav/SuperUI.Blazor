namespace SuperUI.Components;

/// <summary>
/// Default implementation of <see cref="IPermissionService"/> that grants all permissions.
/// Replace with your own implementation to integrate with your authentication system.
/// </summary>
internal sealed class DefaultPermissionService : IPermissionService
{
    public Task<bool> HasPermissionAsync(string permission) => Task.FromResult(true);
    public Task<bool> IsInRoleAsync(string role) => Task.FromResult(true);
    public Task<IEnumerable<string>> GetUserPermissionsAsync() => Task.FromResult(Enumerable.Empty<string>());
    public Task<IEnumerable<string>> GetUserRolesAsync() => Task.FromResult(Enumerable.Empty<string>());
}
