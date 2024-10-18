﻿#nullable enable

using Bit.Core.AdminConsole.Entities;
using Bit.Core.AdminConsole.Enums;
using Bit.Core.AdminConsole.OrganizationFeatures.Policies.Models;
using Bit.Core.Auth.Enums;
using Bit.Core.Auth.Repositories;
using Bit.Core.Services;

namespace Bit.Core.AdminConsole.OrganizationFeatures.Policies.PolicyValidators;

public class RequireSsoPolicyValidator : IPolicyValidator
{
    private readonly ISsoConfigRepository _ssoConfigRepository;

    public RequireSsoPolicyValidator(ISsoConfigRepository ssoConfigRepository)
    {
        _ssoConfigRepository = ssoConfigRepository;
    }

    public PolicyType Type => PolicyType.RequireSso;
    public IEnumerable<PolicyType> RequiredPolicies => [PolicyType.SingleOrg];

    public async Task<string> ValidateAsync(PolicyUpdate policyUpdate, Policy? currentPolicy)
    {
        if (policyUpdate is not { Enabled: true })
        {
            var ssoConfig = await _ssoConfigRepository.GetByOrganizationIdAsync(policyUpdate.OrganizationId);
            return ssoConfig.ValidateDecryptionOptionsNotEnabled([
                MemberDecryptionType.KeyConnector,
                MemberDecryptionType.TrustedDeviceEncryption
            ]);
        }

        return "";
    }

    public Task OnSaveSideEffectsAsync(PolicyUpdate policyUpdate, Policy? currentPolicy, IOrganizationService organizationService) => Task.FromResult(0);
}
