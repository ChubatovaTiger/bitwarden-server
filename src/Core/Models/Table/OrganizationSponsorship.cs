using System;
using System.ComponentModel.DataAnnotations;
using Bit.Core.Utilities;

namespace Bit.Core.Models.Table
{
    public class OrganizationSponsorship : ITableObject<Guid>
    {
        public Guid Id { get; set; }
        public Guid? InstallationId { get; set; }
        [Required]
        public Guid SponsoringOrganizationId { get; set; }
        [Required]
        public Guid SponsoringOrganizationUserId { get; set; }
        public Guid? SponsoredOrganizationId { get; set; }
        [MaxLength(256)]
        public string OfferedToEmail { get; set; }
        [Required]
        public bool CloudSponsor { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public byte TimesRenewedWithoutValidation { get; set; }
        public DateTime? SponsorshipLapsedDate { get; set; }

        public void SetNewId()
        {
            Id = CoreHelpers.GenerateComb();
        }
    }
}
