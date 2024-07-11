﻿using System.Security.Claims;
using Bit.Api.Billing.Controllers;
using Bit.Api.Billing.Models.Requests;
using Bit.Core.AdminConsole.Entities;
using Bit.Core.AdminConsole.Entities.Provider;
using Bit.Core.AdminConsole.Repositories;
using Bit.Core.AdminConsole.Services;
using Bit.Core.Billing.Services;
using Bit.Core.Entities;
using Bit.Core.Models.Business;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Test.Common.AutoFixture;
using Bit.Test.Common.AutoFixture.Attributes;
using Microsoft.AspNetCore.Http.HttpResults;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Xunit;

using static Bit.Api.Test.Billing.Utilities;

namespace Bit.Api.Test.Billing.Controllers;

[ControllerCustomize(typeof(ProviderClientsController))]
[SutProviderCustomize]
public class ProviderClientsControllerTests
{
    #region CreateAsync

    [Theory, BitAutoData]
    public async Task CreateAsync_NoPrincipalUser_Unauthorized(
        Provider provider,
        CreateClientOrganizationRequestBody requestBody,
        SutProvider<ProviderClientsController> sutProvider)
    {
        ConfigureStableAdminInputs(provider, sutProvider);

        sutProvider.GetDependency<IUserService>().GetUserByPrincipalAsync(Arg.Any<ClaimsPrincipal>()).ReturnsNull();

        var result = await sutProvider.Sut.CreateAsync(provider.Id, requestBody);

        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Theory, BitAutoData]
    public async Task CreateAsync_MissingClientOrganization_ServerError(
        Provider provider,
        CreateClientOrganizationRequestBody requestBody,
        SutProvider<ProviderClientsController> sutProvider)
    {
        ConfigureStableAdminInputs(provider, sutProvider);

        var user = new User();

        sutProvider.GetDependency<IUserService>().GetUserByPrincipalAsync(Arg.Any<ClaimsPrincipal>()).Returns(user);

        var clientOrganizationId = Guid.NewGuid();

        sutProvider.GetDependency<IProviderService>().CreateOrganizationAsync(
                provider.Id,
                Arg.Any<OrganizationSignup>(),
                requestBody.OwnerEmail,
                user)
            .Returns(new ProviderOrganization
            {
                OrganizationId = clientOrganizationId
            });

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(clientOrganizationId).ReturnsNull();

        var result = await sutProvider.Sut.CreateAsync(provider.Id, requestBody);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Theory, BitAutoData]
    public async Task CreateAsync_OK(
        Provider provider,
        CreateClientOrganizationRequestBody requestBody,
        SutProvider<ProviderClientsController> sutProvider)
    {
        ConfigureStableAdminInputs(provider, sutProvider);

        var user = new User();

        sutProvider.GetDependency<IUserService>().GetUserByPrincipalAsync(Arg.Any<ClaimsPrincipal>())
            .Returns(user);

        var clientOrganizationId = Guid.NewGuid();

        sutProvider.GetDependency<IProviderService>().CreateOrganizationAsync(
                provider.Id,
                Arg.Is<OrganizationSignup>(signup =>
                    signup.Name == requestBody.Name &&
                    signup.Plan == requestBody.PlanType &&
                    signup.AdditionalSeats == requestBody.Seats &&
                    signup.OwnerKey == requestBody.Key &&
                    signup.PublicKey == requestBody.KeyPair.PublicKey &&
                    signup.PrivateKey == requestBody.KeyPair.EncryptedPrivateKey &&
                    signup.CollectionName == requestBody.CollectionName),
                requestBody.OwnerEmail,
                user)
            .Returns(new ProviderOrganization
            {
                OrganizationId = clientOrganizationId
            });

        var clientOrganization = new Organization { Id = clientOrganizationId };

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(clientOrganizationId)
            .Returns(clientOrganization);

        var result = await sutProvider.Sut.CreateAsync(provider.Id, requestBody);

        Assert.IsType<Ok>(result);

        await sutProvider.GetDependency<IProviderBillingService>().Received(1).CreateCustomerForClientOrganization(
            provider,
            clientOrganization);
    }

    #endregion

    #region UpdateAsync

    [Theory, BitAutoData]
    public async Task UpdateAsync_NoProviderOrganization_NotFound(
        Provider provider,
        Guid providerOrganizationId,
        UpdateClientOrganizationRequestBody requestBody,
        SutProvider<ProviderClientsController> sutProvider)
    {
        ConfigureStableServiceUserInputs(provider, sutProvider);

        sutProvider.GetDependency<IProviderOrganizationRepository>().GetByIdAsync(providerOrganizationId)
            .ReturnsNull();

        var result = await sutProvider.Sut.UpdateAsync(provider.Id, providerOrganizationId, requestBody);

        Assert.IsType<NotFound>(result);
    }

    [Theory, BitAutoData]
    public async Task UpdateAsync_NoOrganization_ServerError(
        Provider provider,
        Guid providerOrganizationId,
        UpdateClientOrganizationRequestBody requestBody,
        ProviderOrganization providerOrganization,
        SutProvider<ProviderClientsController> sutProvider)
    {
        ConfigureStableServiceUserInputs(provider, sutProvider);

        sutProvider.GetDependency<IProviderOrganizationRepository>().GetByIdAsync(providerOrganizationId)
            .Returns(providerOrganization);

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(providerOrganization.OrganizationId)
            .ReturnsNull();

        var result = await sutProvider.Sut.UpdateAsync(provider.Id, providerOrganizationId, requestBody);

        Assert.IsType<ProblemHttpResult>(result);
    }

    [Theory, BitAutoData]
    public async Task UpdateAsync_AssignedSeats_NoContent(
        Provider provider,
        Guid providerOrganizationId,
        UpdateClientOrganizationRequestBody requestBody,
        ProviderOrganization providerOrganization,
        Organization organization,
        SutProvider<ProviderClientsController> sutProvider)
    {
        ConfigureStableServiceUserInputs(provider, sutProvider);

        sutProvider.GetDependency<IProviderOrganizationRepository>().GetByIdAsync(providerOrganizationId)
            .Returns(providerOrganization);

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(providerOrganization.OrganizationId)
            .Returns(organization);

        var result = await sutProvider.Sut.UpdateAsync(provider.Id, providerOrganizationId, requestBody);

        await sutProvider.GetDependency<IProviderBillingService>().Received(1)
            .AssignSeatsToClientOrganization(
                provider,
                organization,
                requestBody.AssignedSeats);

        await sutProvider.GetDependency<IOrganizationRepository>().Received(1)
            .ReplaceAsync(Arg.Is<Organization>(org => org.Name == requestBody.Name));

        Assert.IsType<Ok>(result);
    }

    [Theory, BitAutoData]
    public async Task UpdateAsync_Name_NoContent(
        Provider provider,
        Guid providerOrganizationId,
        UpdateClientOrganizationRequestBody requestBody,
        ProviderOrganization providerOrganization,
        Organization organization,
        SutProvider<ProviderClientsController> sutProvider)
    {
        ConfigureStableServiceUserInputs(provider, sutProvider);

        sutProvider.GetDependency<IProviderOrganizationRepository>().GetByIdAsync(providerOrganizationId)
            .Returns(providerOrganization);

        sutProvider.GetDependency<IOrganizationRepository>().GetByIdAsync(providerOrganization.OrganizationId)
            .Returns(organization);

        requestBody.AssignedSeats = organization.Seats!.Value;

        var result = await sutProvider.Sut.UpdateAsync(provider.Id, providerOrganizationId, requestBody);

        await sutProvider.GetDependency<IProviderBillingService>().DidNotReceiveWithAnyArgs()
            .AssignSeatsToClientOrganization(
                Arg.Any<Provider>(),
                Arg.Any<Organization>(),
                Arg.Any<int>());

        await sutProvider.GetDependency<IOrganizationRepository>().Received(1)
            .ReplaceAsync(Arg.Is<Organization>(org => org.Name == requestBody.Name));

        Assert.IsType<Ok>(result);
    }

    #endregion
}
