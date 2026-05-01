namespace SuperUI.Components;

/// <summary>
/// Service interface for checking user permissions and roles.
/// Implement this interface to integrate with your authentication system.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks if the current user has the specified permission.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    Task<bool> HasPermissionAsync(string permission);

    /// <summary>
    /// Checks if the current user is in the specified role.
    /// </summary>
    /// <param name="role">The role to check.</param>
    /// <returns>True if the user is in the role; otherwise, false.</returns>
    Task<bool> IsInRoleAsync(string role);

    /// <summary>
    /// Gets all permissions for the current user.
    /// </summary>
    /// <returns>A collection of permission names.</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync();

    /// <summary>
    /// Gets all roles for the current user.
    /// </summary>
    /// <returns>A collection of role names.</returns>
    Task<IEnumerable<string>> GetUserRolesAsync();
}
