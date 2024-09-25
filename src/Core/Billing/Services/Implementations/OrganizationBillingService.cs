﻿using Bit.Core.AdminConsole.Entities;
using Bit.Core.Billing.Caches;
using Bit.Core.Billing.Constants;
using Bit.Core.Billing.Models;
using Bit.Core.Billing.Models.Sales;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Utilities;
using Braintree;
using Microsoft.Extensions.Logging;
using Stripe;

using static Bit.Core.Billing.Utilities;
using Customer = Stripe.Customer;
using Subscription = Stripe.Subscription;

namespace Bit.Core.Billing.Services.Implementations;

#nullable enable

public class OrganizationBillingService(
    IBraintreeGateway braintreeGateway,
    IGlobalSettings globalSettings,
    ILogger<OrganizationBillingService> logger,
    IOrganizationRepository organizationRepository,
    ISetupIntentCache setupIntentCache,
    IStripeAdapter stripeAdapter,
    ISubscriberService subscriberService) : IOrganizationBillingService
{
    public async Task Finalize(OrganizationSale sale)
    {
        var (organization, customerSetup, subscriptionSetup) = sale;

        List<string> expand = ["tax"];

        var customer = customerSetup != null
            ? await CreateCustomerAsync(organization, customerSetup, expand)
            : await subscriberService.GetCustomerOrThrow(organization, new CustomerGetOptions { Expand = expand });

        var subscription = await CreateSubscriptionAsync(organization.Id, customer, subscriptionSetup);

        if (subscription.Status is StripeConstants.SubscriptionStatus.Trialing or StripeConstants.SubscriptionStatus.Active)
        {
            organization.Enabled = true;
            organization.ExpirationDate = subscription.CurrentPeriodEnd;
        }

        organization.Gateway = GatewayType.Stripe;
        organization.GatewayCustomerId = customer.Id;
        organization.GatewaySubscriptionId = subscription.Id;

        await organizationRepository.ReplaceAsync(organization);
    }

    public async Task<OrganizationMetadata?> GetMetadata(Guid organizationId)
    {
        var organization = await organizationRepository.GetByIdAsync(organizationId);

        if (organization == null)
        {
            return null;
        }

        var customer = await subscriberService.GetCustomer(organization, new CustomerGetOptions
        {
            Expand = ["discount.coupon.applies_to"]
        });

        var subscription = await subscriberService.GetSubscription(organization);

        if (customer == null || subscription == null)
        {
            return OrganizationMetadata.Default();
        }

        var isOnSecretsManagerStandalone = IsOnSecretsManagerStandalone(organization, customer, subscription);

        return new OrganizationMetadata(isOnSecretsManagerStandalone);
    }

    public async Task UpdatePaymentMethod(
        Organization organization,
        TokenizedPaymentSource tokenizedPaymentSource,
        TaxInformation taxInformation)
    {
        if (string.IsNullOrEmpty(organization.GatewayCustomerId))
        {
            var customer = await CreateCustomerAsync(organization,
                new PaymentSetup
                {
                    TokenizedPaymentSource = tokenizedPaymentSource,
                    TaxInformation = taxInformation
                });

            organization.Gateway = GatewayType.Stripe;
            organization.GatewayCustomerId = customer.Id;

            await organizationRepository.ReplaceAsync(organization);
        }
        else
        {
            await subscriberService.UpdatePaymentSource(organization, tokenizedPaymentSource);
            await subscriberService.UpdateTaxInformation(organization, taxInformation);
        }
    }

    #region Utilities

    private async Task<Customer> CreateCustomerAsync(
        Organization organization,
        PaymentSetup paymentSetup,
        List<string>? expand = null)
    {
        var customerCreateOptions = new CustomerCreateOptions
        {
            Coupon = paymentSetup.Coupon,
            Description = organization.DisplayBusinessName(),
            Email = organization.BillingEmail,
            Expand = expand,
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                CustomFields = [
                    new CustomerInvoiceSettingsCustomFieldOptions
                    {
                        Name = organization.SubscriberType(),
                        Value = organization.DisplayName()[..30]
                    }]
            },
            Metadata = new Dictionary<string, string>
            {
                ["organizationId"] = organization.Id.ToString(),
                ["region"] = globalSettings.BaseServiceUri.CloudRegion
            }
        };

        var braintreeCustomerId = "";

        if (paymentSetup.IsBillable)
        {
            if (paymentSetup.TokenizedPaymentSource is not
                {
                    Type: PaymentMethodType.BankAccount or PaymentMethodType.Card or PaymentMethodType.PayPal,
                    Token: not null and not ""
                })
            {
                logger.LogError(
                    "Cannot create customer for organization ({OrganizationID}) without a valid payment source",
                    organization.Id);

                throw new BillingException();
            }

            if (paymentSetup.TaxInformation is not { Country: not null and not "", PostalCode: not null and not "" })
            {
                logger.LogError(
                    "Cannot create customer for organization ({OrganizationID}) without valid tax information",
                    organization.Id);

                throw new BillingException();
            }

            var (address, taxIdData) = paymentSetup.TaxInformation.GetStripeOptions();

            customerCreateOptions.Address = address;
            customerCreateOptions.Tax = new CustomerTaxOptions
            {
                ValidateLocation = StripeConstants.ValidateTaxLocationTiming.Immediately
            };
            customerCreateOptions.TaxIdData = taxIdData;

            var (type, token) = paymentSetup.TokenizedPaymentSource;

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (type)
            {
                case PaymentMethodType.BankAccount:
                    {
                        var setupIntent =
                            (await stripeAdapter.SetupIntentList(new SetupIntentListOptions { PaymentMethod = token }))
                            .FirstOrDefault();

                        if (setupIntent == null)
                        {
                            logger.LogError("Cannot create customer for organization ({OrganizationID}) without a setup intent for their bank account", organization.Id);

                            throw new BillingException();
                        }

                        await setupIntentCache.Set(organization.Id, setupIntent.Id);

                        break;
                    }
                case PaymentMethodType.Card:
                    {
                        customerCreateOptions.PaymentMethod = token;
                        customerCreateOptions.InvoiceSettings.DefaultPaymentMethod = token;
                        break;
                    }
                case PaymentMethodType.PayPal:
                    {
                        braintreeCustomerId = await subscriberService.CreateBraintreeCustomer(organization, token);
                        customerCreateOptions.Metadata[BraintreeCustomerIdKey] = braintreeCustomerId;
                        break;
                    }
                default:
                    {
                        logger.LogError("Cannot create customer for organization ({OrganizationID}) using payment method type ({PaymentMethodType}) as it is not supported", organization.Id, type.ToString());
                        throw new BillingException();
                    }
            }
        }

        try
        {
            return await stripeAdapter.CustomerCreateAsync(customerCreateOptions);
        }
        catch (StripeException stripeException) when (stripeException.StripeError?.Code ==
                                                      StripeConstants.ErrorCodes.CustomerTaxLocationInvalid)
        {
            await Revert();
            throw new BadRequestException(
                "Your location wasn't recognized. Please ensure your country and postal code are valid.");
        }
        catch (StripeException stripeException) when (stripeException.StripeError?.Code ==
                                                      StripeConstants.ErrorCodes.TaxIdInvalid)
        {
            await Revert();
            throw new BadRequestException(
                "Your tax ID wasn't recognized for your selected country. Please ensure your country and tax ID are valid.");
        }
        catch
        {
            await Revert();
            throw;
        }

        async Task Revert()
        {
            if (paymentSetup.IsBillable)
            {
                // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                switch (paymentSetup.TokenizedPaymentSource!.Type)
                {
                    case PaymentMethodType.BankAccount:
                        {
                            await setupIntentCache.Remove(organization.Id);
                            break;
                        }
                    case PaymentMethodType.PayPal:
                        {
                            await braintreeGateway.Customer.DeleteAsync(braintreeCustomerId);
                            break;
                        }
                }
            }
        }
    }

    private async Task<Subscription> CreateSubscriptionAsync(
        Guid organizationId,
        Customer customer,
        SubscriptionSetup subscriptionSetup)
    {
        var plan = subscriptionSetup.Plan;

        var passwordManagerOptions = subscriptionSetup.PasswordManagerOptions;

        var subscriptionItemOptionsList = new List<SubscriptionItemOptions>
        {
            plan.HasNonSeatBasedPasswordManagerPlan()
                ? new SubscriptionItemOptions
                {
                    Price = plan.PasswordManager.StripePlanId,
                    Quantity = 1
                }
                : new SubscriptionItemOptions
                {
                    Price = plan.PasswordManager.StripeSeatPlanId,
                    Quantity = passwordManagerOptions.Seats
                }
        };

        if (passwordManagerOptions.PremiumAccess is true)
        {
            subscriptionItemOptionsList.Add(new SubscriptionItemOptions
            {
                Price = plan.PasswordManager.StripePremiumAccessPlanId,
                Quantity = 1
            });
        }

        if (passwordManagerOptions.Storage is > 0)
        {
            subscriptionItemOptionsList.Add(new SubscriptionItemOptions
            {
                Price = plan.PasswordManager.StripeStoragePlanId,
                Quantity = passwordManagerOptions.Storage
            });
        }

        var secretsManagerOptions = subscriptionSetup.SecretsManagerOptions;

        if (secretsManagerOptions != null)
        {
            subscriptionItemOptionsList.Add(new SubscriptionItemOptions
            {
                Price = plan.SecretsManager.StripeSeatPlanId,
                Quantity = secretsManagerOptions.Seats
            });

            if (secretsManagerOptions.ServiceAccounts is > 0)
            {
                subscriptionItemOptionsList.Add(new SubscriptionItemOptions
                {
                    Price = plan.SecretsManager.StripeServiceAccountPlanId,
                    Quantity = secretsManagerOptions.ServiceAccounts
                });
            }
        }

        var subscriptionCreateOptions = new SubscriptionCreateOptions
        {
            AutomaticTax = new SubscriptionAutomaticTaxOptions
            {
                Enabled = customer.Tax?.AutomaticTax == StripeConstants.AutomaticTaxStatus.Supported
            },
            CollectionMethod = StripeConstants.CollectionMethod.ChargeAutomatically,
            Customer = customer.Id,
            Items = subscriptionItemOptionsList,
            Metadata = new Dictionary<string, string>
            {
                ["organizationId"] = organizationId.ToString()
            },
            OffSession = true,
            TrialPeriodDays = plan.TrialPeriodDays
        };

        return await stripeAdapter.SubscriptionCreateAsync(subscriptionCreateOptions);
    }

    private static bool IsOnSecretsManagerStandalone(
        Organization organization,
        Customer customer,
        Subscription subscription)
    {
        var plan = StaticStore.GetPlan(organization.PlanType);

        if (!plan.SupportsSecretsManager)
        {
            return false;
        }

        var hasCoupon = customer.Discount?.Coupon?.Id == StripeConstants.CouponIDs.SecretsManagerStandalone;

        if (!hasCoupon)
        {
            return false;
        }

        var subscriptionProductIds = subscription.Items.Data.Select(item => item.Plan.ProductId);

        var couponAppliesTo = customer.Discount?.Coupon?.AppliesTo?.Products;

        return subscriptionProductIds.Intersect(couponAppliesTo ?? []).Any();
    }

    #endregion
}
