﻿using Bit.Scim.Groups;
using Bit.Scim.Groups.Interfaces;
using Bit.Scim.Users;
using Bit.Scim.Users.Interfaces;

namespace Bit.Scim.Utilities;

public static class ScimServiceCollectionExtensions
{
    public static void AddScimGroupCommands(this IServiceCollection services)
    {
        services.AddScoped<IPostGroupCommand, PostGroupCommand>();
    }

    public static void AddScimUserQueries(this IServiceCollection services)
    {
        services.AddScoped<IGetUserQuery, GetUserQuery>();
    }
}
