﻿using Bit.Core.Billing.Caches;
using Bit.Core.Billing.Caches.Implementations;
using Bit.Core.Billing.Services;
using Bit.Core.Billing.Services.Implementations;

namespace Bit.Core.Billing.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddBillingOperations(this IServiceCollection services)
    {
        services.AddTransient<IPremiumBillingService, PremiumBillingService>();
        services.AddTransient<IOrganizationBillingService, OrganizationBillingService>();
        services.AddTransient<ISetupIntentCache, SetupIntentDistributedCache>();
        services.AddTransient<ISubscriberService, SubscriberService>();
    }
}
