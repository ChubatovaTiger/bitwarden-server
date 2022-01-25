using System;
using System.Threading.Tasks;

namespace Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Interfaces
{
    public interface IValidateSponsorshipCommand
    {
        Task<bool> ValidateSponsorshipAsync(Guid sponsoredOrganizationId);
    }
}
