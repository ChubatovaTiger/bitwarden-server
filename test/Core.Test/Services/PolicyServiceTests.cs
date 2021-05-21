﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bit.Core.Exceptions;
using Bit.Core.Models.Table;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Test.AutoFixture;
using Bit.Core.Test.AutoFixture.Attributes;
using Braintree.Test;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.Services
{
    public class PolicyServiceTests
    {
        [Theory, PolicyAutoData]
        public async Task SaveAsync_OrganizationDoesNotExist_ThrowsBadRequest(Policy policy, SutProvider<PolicyService> sutProvider)
        {
            SetupOrg(sutProvider, policy.OrganizationId, null);

            var badRequestException = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.SaveAsync(policy,
                    Substitute.For<IUserService>(),
                    Substitute.For<IOrganizationService>(),
                    Guid.NewGuid()));

            Assert.Contains("Organization not found", badRequestException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory, PolicyAutoData]
        public async Task SaveAsync_OrganizationCannotUsePolicies_ThrowsBadRequest(Policy policy, SutProvider<PolicyService> sutProvider)
        {
            var orgId = Guid.NewGuid();

            SetupOrg(sutProvider, policy.OrganizationId, new Organization 
            {
                UsePolicies = false,
            });

            var badRequestException = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.SaveAsync(policy,
                    Substitute.For<IUserService>(),
                    Substitute.For<IOrganizationService>(),
                    Guid.NewGuid()));

            Assert.Contains("cannot use policies", badRequestException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory, PolicyAutoData]
        public async Task SaveAsync_SingleOrg_RequireSsoEnabled_ThrowsBadRequest(Policy policy, SutProvider<PolicyService> sutProvider)
        {
            policy.Type = Enums.PolicyType.SingleOrg;
            policy.Enabled = false;

            SetupOrg(sutProvider, policy.OrganizationId, new Organization
            {
                Id = policy.OrganizationId,
                UsePolicies = true,
            });

            sutProvider.GetDependency<IPolicyRepository>()
                .GetByOrganizationIdTypeAsync(policy.OrganizationId, Enums.PolicyType.RequireSso)
                .Returns(Task.FromResult(new Policy { Enabled = true }));

            var badRequestException = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.SaveAsync(policy,
                    Substitute.For<IUserService>(),
                    Substitute.For<IOrganizationService>(),
                    Guid.NewGuid()));

            Assert.Contains("Single Sign-On Authentication policy is enabled.", badRequestException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory, PolicyAutoData]
        public async Task SaveAsync_RequireSsoPolicy_NotEnabled_ThrowsBadRequestAsync(Policy policy, SutProvider<PolicyService> sutProvider)
        {
            policy.Type = Enums.PolicyType.RequireSso;
            policy.Enabled = true;

            SetupOrg(sutProvider, policy.OrganizationId, new Organization
            {
                Id = policy.OrganizationId,
                UsePolicies = true,
            });

            sutProvider.GetDependency<IPolicyRepository>()
                .GetByOrganizationIdTypeAsync(policy.OrganizationId, Enums.PolicyType.SingleOrg)
                .Returns(Task.FromResult(new Policy { Enabled = false }));

            var badRequestException = await Assert.ThrowsAsync<BadRequestException>(
                () => sutProvider.Sut.SaveAsync(policy,
                    Substitute.For<IUserService>(),
                    Substitute.For<IOrganizationService>(),
                    Guid.NewGuid()));

            Assert.Contains("Single Organization policy not enabled.", badRequestException.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory, PolicyAutoData]
        public async Task SaveAsync_NewPolicy_Created(Policy policy, SutProvider<PolicyService> sutProvider)
        {
            policy.Id = default;
            policy.Type = Enums.PolicyType.MasterPassword;

            SetupOrg(sutProvider, policy.OrganizationId, new Organization
            {
                Id = policy.OrganizationId,
                UsePolicies = true,
            });

            var utcNow = DateTime.UtcNow;

            await sutProvider.Sut.SaveAsync(policy, Substitute.For<IUserService>(), Substitute.For<IOrganizationService>(), Guid.NewGuid());

            await sutProvider.GetDependency<IEventService>().Received()
                .LogPolicyEventAsync(policy, Enums.EventType.Policy_Updated);

            await sutProvider.GetDependency<IPolicyRepository>().Received()
                .UpsertAsync(policy);

            Assert.True(policy.CreationDate - utcNow < TimeSpan.FromSeconds(1));
            Assert.True(policy.RevisionDate - utcNow < TimeSpan.FromSeconds(1));
        }

        [Theory, PolicyAutoData]
        public async Task SaveAsync_ExistingPolicy_UpdateTwoFactor(Policy policy, SutProvider<PolicyService> sutProvider)
        {
            // If the policy that this is updating isn't enabled then do some work now that the current one is enabled
            policy.Type = Enums.PolicyType.TwoFactorAuthentication;

            var org = new Organization
            {
                Id = policy.OrganizationId,
                UsePolicies = true,
                Name = "TEST",
            };

            SetupOrg(sutProvider, policy.OrganizationId, org);

            sutProvider.GetDependency<IPolicyRepository>()
                .GetByIdAsync(policy.Id)
                .Returns(new Policy
                { 
                    Id = policy.Id,
                    Type = Enums.PolicyType.TwoFactorAuthentication,
                    Enabled = false,
                });

            var orgUserDetail = new Core.Models.Data.OrganizationUserUserDetails
            {
                Id = Guid.NewGuid(),
                Status = Enums.OrganizationUserStatusType.Accepted,
                Type = Enums.OrganizationUserType.User,
                // Needs to be different from what is passed in as the savingUserId to Sut.SaveAsync
                Email = "test@bitwarden.com",
                Name = "TEST",
                UserId = Guid.NewGuid(),
            };

            sutProvider.GetDependency<IOrganizationUserRepository>()
                .GetManyDetailsByOrganizationAsync(policy.OrganizationId)
                .Returns(new List<Core.Models.Data.OrganizationUserUserDetails>
                {
                    orgUserDetail,
                });

            var userService = Substitute.For<IUserService>();
            var organizationService = Substitute.For<IOrganizationService>();

            userService.TwoFactorIsEnabledAsync(orgUserDetail)
                .Returns(false);

            var utcNow = DateTime.UtcNow;

            var savingUserId = Guid.NewGuid();

            await sutProvider.Sut.SaveAsync(policy, userService, organizationService, savingUserId);

            await organizationService.Received()
                .DeleteUserAsync(policy.OrganizationId, orgUserDetail.Id, savingUserId);

            await sutProvider.GetDependency<IMailService>().Received()
                .SendOrganizationUserRemovedForPolicyTwoStepEmailAsync(org.Name, orgUserDetail.Email);

            await sutProvider.GetDependency<IEventService>().Received()
                .LogPolicyEventAsync(policy, Enums.EventType.Policy_Updated);

            await sutProvider.GetDependency<IPolicyRepository>().Received()
                .UpsertAsync(policy);

            Assert.True(policy.CreationDate - utcNow < TimeSpan.FromSeconds(1));
            Assert.True(policy.RevisionDate - utcNow < TimeSpan.FromSeconds(1));
        }

        [Theory, PolicyAutoData]
        public async Task SaveAsync_ExistingPolicy_UpdateSingleOrg(Policy policy, SutProvider<PolicyService> sutProvider)
        {
            // If the policy that this is updating isn't enabled then do some work now that the current one is enabled
            policy.Type = Enums.PolicyType.TwoFactorAuthentication;

            var org = new Organization
            {
                Id = policy.OrganizationId,
                UsePolicies = true,
                Name = "TEST",
            };

            SetupOrg(sutProvider, policy.OrganizationId, org);

            sutProvider.GetDependency<IPolicyRepository>()
                .GetByIdAsync(policy.Id)
                .Returns(new Policy
                {
                    Id = policy.Id,
                    Type = Enums.PolicyType.SingleOrg,
                    Enabled = false,
                });

            var orgUserDetail = new Core.Models.Data.OrganizationUserUserDetails
            {
                Id = Guid.NewGuid(),
                Status = Enums.OrganizationUserStatusType.Accepted,
                Type = Enums.OrganizationUserType.User,
                // Needs to be different from what is passed in as the savingUserId to Sut.SaveAsync
                Email = "test@bitwarden.com",
                Name = "TEST",
                UserId = Guid.NewGuid(),
            };

            sutProvider.GetDependency<IOrganizationUserRepository>()
                .GetManyDetailsByOrganizationAsync(policy.OrganizationId)
                .Returns(new List<Core.Models.Data.OrganizationUserUserDetails>
                {
                    orgUserDetail,
                });

            var userService = Substitute.For<IUserService>();
            var organizationService = Substitute.For<IOrganizationService>();

            userService.TwoFactorIsEnabledAsync(orgUserDetail)
                .Returns(false);

            var utcNow = DateTime.UtcNow;

            var savingUserId = Guid.NewGuid();

            await sutProvider.Sut.SaveAsync(policy, userService, organizationService, savingUserId);

            await sutProvider.GetDependency<IEventService>().Received()
                .LogPolicyEventAsync(policy, Enums.EventType.Policy_Updated);

            await sutProvider.GetDependency<IPolicyRepository>().Received()
                .UpsertAsync(policy);

            Assert.True(policy.CreationDate - utcNow < TimeSpan.FromSeconds(1));
            Assert.True(policy.RevisionDate - utcNow < TimeSpan.FromSeconds(1));
        }

        private static void SetupOrg(SutProvider<PolicyService> sutProvider, Guid organizationId, Organization organization)
        {
            sutProvider.GetDependency<IOrganizationRepository>()
                .GetByIdAsync(organizationId)
                .Returns(Task.FromResult(organization));
        }
    }
}
