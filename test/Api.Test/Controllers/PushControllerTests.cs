﻿#nullable enable
using Bit.Api.Controllers;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Api;
using Bit.Core.Services;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using NSubstitute;
using Xunit;
using GlobalSettings = Bit.Core.Settings.GlobalSettings;

namespace Bit.Api.Test.Controllers;

[ControllerCustomize(typeof(PushController))]
[SutProviderCustomize]
public class PushControllerTests
{
    [Theory]
    [BitAutoData(false, true)]
    [BitAutoData(false, false)]
    [BitAutoData(true, true)]
    public async Task SendAsync_InstallationIdNotSetOrSelfHosted_BadRequest(bool haveInstallationId, bool selfHosted,
        SutProvider<PushController> sutProvider, Guid installationId, Guid userId, Guid organizationId)
    {
        sutProvider.GetDependency<GlobalSettings>().SelfHosted = selfHosted;
        if (haveInstallationId)
        {
            sutProvider.GetDependency<ICurrentContext>().InstallationId.Returns(installationId);
        }

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            sutProvider.Sut.PostSend(new PushSendRequestModel
            {
                Type = PushType.SyncNotificationCreate,
                UserId = userId.ToString(),
                OrganizationId = organizationId.ToString(),
                Payload = "test-payload"
            }));

        Assert.Equal("Not correctly configured for push relays.", exception.Message);

        await sutProvider.GetDependency<IPushNotificationService>().Received(0)
            .SendPayloadToUserAsync(Arg.Any<string>(), Arg.Any<PushType>(), Arg.Any<object>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<ClientType?>());
        await sutProvider.GetDependency<IPushNotificationService>().Received(0)
            .SendPayloadToOrganizationAsync(Arg.Any<string>(), Arg.Any<PushType>(), Arg.Any<object>(),
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<ClientType?>());
    }

    [Theory]
    [BitAutoData]
    public async Task SendAsync_UserIdAndOrganizationIdEmpty_NoPushNotificationSent(
        SutProvider<PushController> sutProvider, Guid installationId)
    {
        sutProvider.GetDependency<GlobalSettings>().SelfHosted = false;
        sutProvider.GetDependency<ICurrentContext>().InstallationId.Returns(installationId);

        await sutProvider.Sut.PostSend(new PushSendRequestModel
        {
            Type = PushType.SyncNotificationCreate,
            UserId = null,
            OrganizationId = null,
            Payload = "test-payload"
        });

        await sutProvider.GetDependency<IPushNotificationService>().Received(0)
            .SendPayloadToUserAsync(Arg.Any<string>(), Arg.Any<PushType>(), Arg.Any<object>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<ClientType?>());
        await sutProvider.GetDependency<IPushNotificationService>().Received(0)
            .SendPayloadToOrganizationAsync(Arg.Any<string>(), Arg.Any<PushType>(), Arg.Any<object>(),
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<ClientType?>());
    }

    [Theory]
    [BitAutoData(true, true, false)]
    [BitAutoData(true, true, true)]
    [BitAutoData(true, false, true)]
    [BitAutoData(true, false, false)]
    [BitAutoData(false, true, true)]
    [BitAutoData(false, true, false)]
    [BitAutoData(false, false, true)]
    [BitAutoData(false, false, false)]
    public async Task SendAsync_UserIdSet_SendPayloadToUserAsync(bool haveIdentifier, bool haveDeviceId,
        bool haveOrganizationId, SutProvider<PushController> sutProvider, Guid installationId, Guid userId,
        Guid identifier, Guid deviceId)
    {
        sutProvider.GetDependency<GlobalSettings>().SelfHosted = false;
        sutProvider.GetDependency<ICurrentContext>().InstallationId.Returns(installationId);

        var expectedUserId = $"{installationId}_{userId}";
        var expectedIdentifier = haveIdentifier ? $"{installationId}_{identifier}" : null;
        var expectedDeviceId = haveDeviceId ? $"{installationId}_{deviceId}" : null;

        await sutProvider.Sut.PostSend(new PushSendRequestModel
        {
            Type = PushType.SyncNotificationCreate,
            UserId = userId.ToString(),
            OrganizationId = haveOrganizationId ? Guid.NewGuid().ToString() : null,
            Payload = "test-payload",
            DeviceId = haveDeviceId ? deviceId.ToString() : null,
            Identifier = haveIdentifier ? identifier.ToString() : null,
            ClientType = ClientType.All,
        });

        await sutProvider.GetDependency<IPushNotificationService>().Received(1)
            .SendPayloadToUserAsync(expectedUserId, PushType.SyncNotificationCreate, "test-payload", expectedIdentifier,
                expectedDeviceId, ClientType.All);
        await sutProvider.GetDependency<IPushNotificationService>().Received(0)
            .SendPayloadToOrganizationAsync(Arg.Any<string>(), Arg.Any<PushType>(), Arg.Any<object>(),
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<ClientType?>());
    }

    [Theory]
    [BitAutoData(true, true)]
    [BitAutoData(true, false)]
    [BitAutoData(false, true)]
    [BitAutoData(false, false)]
    public async Task SendAsync_OrganizationIdSet_SendPayloadToOrganizationAsync(bool haveIdentifier, bool haveDeviceId,
        SutProvider<PushController> sutProvider, Guid installationId, Guid organizationId, Guid identifier,
        Guid deviceId)
    {
        sutProvider.GetDependency<GlobalSettings>().SelfHosted = false;
        sutProvider.GetDependency<ICurrentContext>().InstallationId.Returns(installationId);

        var expectedOrganizationId = $"{installationId}_{organizationId}";
        var expectedIdentifier = haveIdentifier ? $"{installationId}_{identifier}" : null;
        var expectedDeviceId = haveDeviceId ? $"{installationId}_{deviceId}" : null;

        await sutProvider.Sut.PostSend(new PushSendRequestModel
        {
            Type = PushType.SyncNotificationCreate,
            UserId = null,
            OrganizationId = organizationId.ToString(),
            Payload = "test-payload",
            DeviceId = haveDeviceId ? deviceId.ToString() : null,
            Identifier = haveIdentifier ? identifier.ToString() : null,
            ClientType = ClientType.All,
        });

        await sutProvider.GetDependency<IPushNotificationService>().Received(1)
            .SendPayloadToOrganizationAsync(expectedOrganizationId, PushType.SyncNotificationCreate, "test-payload",
                expectedIdentifier, expectedDeviceId, ClientType.All);
        await sutProvider.GetDependency<IPushNotificationService>().Received(0)
            .SendPayloadToUserAsync(Arg.Any<string>(), Arg.Any<PushType>(), Arg.Any<object>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<ClientType?>());
    }
}
