﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bit.Core.Models.Data;

namespace Bit.Core.Models.Api.Response.OrganizationSponsorships
{
    public class OrganizationSponsorshipSyncResponseModel
    {
        public IEnumerable<OrganizationSponsorshipResponseModel> SponsorshipsBatch { get; set; }

        public OrganizationSponsorshipSyncResponseModel() {}

        public OrganizationSponsorshipSyncResponseModel(OrganizationSponsorshipSyncData syncData)
        {
            if (syncData == null)
            {
                return;
            }
            SponsorshipsBatch = syncData.SponsorshipsBatch.Select(o => new OrganizationSponsorshipResponseModel(o));

        }

        public OrganizationSponsorshipSyncData ToOrganizationSponsorshipSync()
        {
            return new OrganizationSponsorshipSyncData()
            {
                SponsorshipsBatch = SponsorshipsBatch.Select(o => o.ToOrganizationSponsorship())
            };
        }

    }
}
