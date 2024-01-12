﻿using Bit.Core.AdminConsole.OrganizationFeatures.OrganizationUsers;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.AdminConsole.OrganizationFeatures.OrganizationUsers;

[SutProviderCustomize]
public class UpdateOrganizationUserGroupsCommandTests
{
    [Theory, BitAutoData]
    public async Task UpdateUserGroups_Passes(
        OrganizationUser organizationUser,
        IEnumerable<Guid> groupIds,
        SutProvider<UpdateOrganizationUserGroupsCommand> sutProvider)
    {
        await sutProvider.Sut.UpdateUserGroupsAsync(organizationUser, groupIds, null);

        await sutProvider.GetDependency<IOrganizationUserRepository>().Received(1)
            .UpdateGroupsAsync(organizationUser.Id, groupIds);
        await sutProvider.GetDependency<IEventService>().Received(1)
            .LogOrganizationUserEventAsync(organizationUser, EventType.OrganizationUser_UpdatedGroups);
    }

    [Theory, BitAutoData]
    public async Task UpdateUserGroups_WithSavingUserId_Passes(
        OrganizationUser organizationUser,
        IEnumerable<Guid> groupIds,
        Guid savingUserId,
        SutProvider<UpdateOrganizationUserGroupsCommand> sutProvider)
    {
        organizationUser.Permissions = null;

        await sutProvider.Sut.UpdateUserGroupsAsync(organizationUser, groupIds, savingUserId);

        await sutProvider.GetDependency<IOrganizationUserRepository>().Received(1)
            .UpdateGroupsAsync(organizationUser.Id, groupIds);
        await sutProvider.GetDependency<IEventService>().Received(1)
            .LogOrganizationUserEventAsync(organizationUser, EventType.OrganizationUser_UpdatedGroups);
    }
}
