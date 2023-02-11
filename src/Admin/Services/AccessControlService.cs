﻿using Bit.Admin.Enums;
using Bit.Admin.Utilities;
using Bit.Core.Settings;

namespace Bit.Admin.Services;

public class AccessControlService : IAccessControlService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IGlobalSettings _globalSettings;

    public AccessControlService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IGlobalSettings globalSettings)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _globalSettings = globalSettings;
    }

    public bool UserHasPermission(Permission permission)
    {
        if (_globalSettings.SelfHosted)
            return true;

        var userRole = GetUserRoleFromClaim();
        if (string.IsNullOrEmpty(userRole) || !RolePermissionMapping.RolePermissions.ContainsKey(userRole))
            return false;

        return RolePermissionMapping.RolePermissions[userRole].Contains(permission);
    }

    public string GetUserRole(string userEmail)
    {
        var settings = _configuration.GetSection("adminSettings").GetChildren();

        if (settings == null || !settings.Any())
            return null;

        var rolePrefix = "role";

        foreach (var setting in settings)
        {
            var key = setting.Key;

            if (setting.Value == null || !key.Contains(rolePrefix))
                continue;

            var usersInRole = setting.Value.ToLowerInvariant().Split(',');

            if (!usersInRole.Contains(userEmail))
                continue;

            var role = key.Substring(key.IndexOf(rolePrefix) + rolePrefix.Length);
            return role;
        }

        return null;
    }

    private string GetUserRoleFromClaim()
    {
        var claims = _httpContextAccessor.HttpContext?.User?.Claims;
        if (claims == null || !claims.Any())
            return null;

        return claims.FirstOrDefault(c => c.Type == "Role")?.Value;
    }
}
