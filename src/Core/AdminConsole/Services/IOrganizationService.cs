﻿using System.Security.Claims;
using Bit.Core.AdminConsole.Entities;
using Bit.Core.AdminConsole.Models.Business;
using Bit.Core.Auth.Enums;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Models.Business;
using Bit.Core.Models.Data;

namespace Bit.Core.Services;

public interface IOrganizationService
{
    Task ReplacePaymentMethodAsync(Guid organizationId, string paymentToken, PaymentMethodType paymentMethodType,
        TaxInfo taxInfo);
    Task CancelSubscriptionAsync(Guid organizationId, bool? endOfPeriod = null);
    Task ReinstateSubscriptionAsync(Guid organizationId);
    Task<string> AdjustStorageAsync(Guid organizationId, short storageAdjustmentGb);
    Task UpdateSubscription(Guid organizationId, int seatAdjustment, int? maxAutoscaleSeats);
    Task AutoAddSeatsAsync(Organization organization, int seatsToAdd);
    Task<string> AdjustSeatsAsync(Guid organizationId, int seatAdjustment);
    Task VerifyBankAsync(Guid organizationId, int amount1, int amount2);
    /// <summary>
    /// Create a new organization in a cloud environment
    /// </summary>
    /// <returns>A tuple containing the new organization, the initial organizationUser (if any) and the default collection (if any)</returns>
#nullable enable
    Task<(Organization organization, OrganizationUser? organizationUser, Collection? defaultCollection)> SignUpAsync(OrganizationSignup organizationSignup);

    Task<(Organization organization, OrganizationUser organizationUser, Collection defaultCollection)> SignupClientAsync(OrganizationSignup signup);
#nullable disable
    /// <summary>
    /// Create a new organization on a self-hosted instance
    /// </summary>
    Task<(Organization organization, OrganizationUser organizationUser)> SignUpAsync(OrganizationLicense license, User owner,
        string ownerKey, string collectionName, string publicKey, string privateKey);
    Task InitiateDeleteAsync(Organization organization, string orgAdminEmail);
    Task DeleteAsync(Organization organization);
    Task EnableAsync(Guid organizationId, DateTime? expirationDate);
    Task DisableAsync(Guid organizationId, DateTime? expirationDate);
    Task UpdateExpirationDateAsync(Guid organizationId, DateTime? expirationDate);
    Task EnableAsync(Guid organizationId);
    Task UpdateAsync(Organization organization, bool updateBilling = false, EventType eventType = EventType.Organization_Updated);
    Task UpdateTwoFactorProviderAsync(Organization organization, TwoFactorProviderType type);
    Task DisableTwoFactorProviderAsync(Organization organization, TwoFactorProviderType type);
    Task<OrganizationUser> InviteUserAsync(Guid organizationId, Guid? invitingUserId, EventSystemUser? systemUser,
        OrganizationUserInvite invite, string externalId);
    Task<List<OrganizationUser>> InviteUsersAsync(Guid organizationId, Guid? invitingUserId, EventSystemUser? systemUser,
        IEnumerable<(OrganizationUserInvite invite, string externalId)> invites);
    Task<IEnumerable<Tuple<OrganizationUser, string>>> ResendInvitesAsync(Guid organizationId, Guid? invitingUserId, IEnumerable<Guid> organizationUsersId);
    Task ResendInviteAsync(Guid organizationId, Guid? invitingUserId, Guid organizationUserId, bool initOrganization = false);
    Task<OrganizationUser> ConfirmUserAsync(Guid organizationId, Guid organizationUserId, string key,
        Guid confirmingUserId, IUserService userService);
    Task<List<Tuple<OrganizationUser, string>>> ConfirmUsersAsync(Guid organizationId, Dictionary<Guid, string> keys,
        Guid confirmingUserId, IUserService userService);
    Task<List<Tuple<OrganizationUser, string>>> ConfirmUsersAsync_vNext(Guid organizationId, Dictionary<Guid, string> keys,
        Guid confirmingUserId);
    [Obsolete("IRemoveOrganizationUserCommand should be used instead. To be removed by EC-607.")]
    Task RemoveUserAsync(Guid organizationId, Guid organizationUserId, Guid? deletingUserId);
    [Obsolete("IRemoveOrganizationUserCommand should be used instead. To be removed by EC-607.")]
    Task RemoveUserAsync(Guid organizationId, Guid organizationUserId, EventSystemUser systemUser);
    Task RemoveUserAsync(Guid organizationId, Guid userId);
    Task<List<Tuple<OrganizationUser, string>>> RemoveUsersAsync(Guid organizationId,
        IEnumerable<Guid> organizationUserIds, Guid? deletingUserId);
    Task UpdateUserResetPasswordEnrollmentAsync(Guid organizationId, Guid userId, string resetPasswordKey, Guid? callingUserId);
    Task ImportAsync(Guid organizationId, IEnumerable<ImportedGroup> groups,
        IEnumerable<ImportedOrganizationUser> newUsers, IEnumerable<string> removeUserExternalIds,
        bool overwriteExisting, EventSystemUser eventSystemUser);
    Task DeleteSsoUserAsync(Guid userId, Guid? organizationId);
    Task<Organization> UpdateOrganizationKeysAsync(Guid orgId, string publicKey, string privateKey);
    Task<bool> HasConfirmedOwnersExceptAsync(Guid organizationId, IEnumerable<Guid> organizationUsersId, bool includeProvider = true);
    Task RevokeUserAsync(OrganizationUser organizationUser, Guid? revokingUserId);
    Task RevokeUserAsync(OrganizationUser organizationUser, EventSystemUser systemUser);
    Task<List<Tuple<OrganizationUser, string>>> RevokeUsersAsync(Guid organizationId,
        IEnumerable<Guid> organizationUserIds, Guid? revokingUserId);
    Task RestoreUserAsync(OrganizationUser organizationUser, Guid? restoringUserId, IUserService userService);
    Task RestoreUserAsync(OrganizationUser organizationUser, EventSystemUser systemUser, IUserService userService);
    Task<List<Tuple<OrganizationUser, string>>> RestoreUsersAsync(Guid organizationId,
        IEnumerable<Guid> organizationUserIds, Guid? restoringUserId, IUserService userService);
    Task CreatePendingOrganization(Organization organization, string ownerEmail, ClaimsPrincipal user, IUserService userService, bool salesAssistedTrialStarted);
    /// <summary>
    /// Update an Organization entry by setting the public/private keys, set it as 'Enabled' and move the Status from 'Pending' to 'Created'.
    /// </summary>
    /// <remarks>
    /// This method must target a disabled Organization that has null keys and status as 'Pending'.
    /// </remarks>
    Task InitPendingOrganization(Guid userId, Guid organizationId, Guid organizationUserId, string publicKey, string privateKey, string collectionName);
    Task ReplaceAndUpdateCacheAsync(Organization org, EventType? orgEvent = null);

    void ValidatePasswordManagerPlan(Models.StaticStore.Plan plan, OrganizationUpgrade upgrade);
    void ValidateSecretsManagerPlan(Models.StaticStore.Plan plan, OrganizationUpgrade upgrade);
    Task ValidateOrganizationUserUpdatePermissions(Guid organizationId, OrganizationUserType newType,
        OrganizationUserType? oldType, Permissions permissions);
    Task ValidateOrganizationCustomPermissionsEnabledAsync(Guid organizationId, OrganizationUserType newType);
}
