﻿using Bit.Core.Billing.Models;
using Bit.Core.Entities;
using Bit.Core.Models.Business;
using Bit.Core.Services;
using Braintree;
using Microsoft.Extensions.Logging;
using Stripe;

using static Bit.Core.Billing.Utilities;
using Customer = Stripe.Customer;
using Subscription = Stripe.Subscription;

namespace Bit.Core.Billing.Services.Implementations;

public class SubscriberService(
    IBraintreeGateway braintreeGateway,
    ILogger<SubscriberService> logger,
    IStripeAdapter stripeAdapter) : ISubscriberService
{
    public async Task CancelSubscription(
        ISubscriber subscriber,
        OffboardingSurveyResponse offboardingSurveyResponse,
        bool cancelImmediately)
    {
        var subscription = await GetSubscriptionOrThrow(subscriber);

        if (subscription.CanceledAt.HasValue ||
            subscription.Status == "canceled" ||
            subscription.Status == "unpaid" ||
            subscription.Status == "incomplete_expired")
        {
            logger.LogWarning("Cannot cancel subscription ({ID}) that's already inactive", subscription.Id);

            throw ContactSupport();
        }

        var metadata = new Dictionary<string, string>
        {
            { "cancellingUserId", offboardingSurveyResponse.UserId.ToString() }
        };

        List<string> validCancellationReasons = [
            "customer_service",
            "low_quality",
            "missing_features",
            "other",
            "switched_service",
            "too_complex",
            "too_expensive",
            "unused"
        ];

        if (cancelImmediately)
        {
            if (subscription.Metadata != null && subscription.Metadata.ContainsKey("organizationId"))
            {
                await stripeAdapter.SubscriptionUpdateAsync(subscription.Id, new SubscriptionUpdateOptions
                {
                    Metadata = metadata
                });
            }

            var options = new SubscriptionCancelOptions
            {
                CancellationDetails = new SubscriptionCancellationDetailsOptions
                {
                    Comment = offboardingSurveyResponse.Feedback
                }
            };

            if (validCancellationReasons.Contains(offboardingSurveyResponse.Reason))
            {
                options.CancellationDetails.Feedback = offboardingSurveyResponse.Reason;
            }

            await stripeAdapter.SubscriptionCancelAsync(subscription.Id, options);
        }
        else
        {
            var options = new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true,
                CancellationDetails = new SubscriptionCancellationDetailsOptions
                {
                    Comment = offboardingSurveyResponse.Feedback
                },
                Metadata = metadata
            };

            if (validCancellationReasons.Contains(offboardingSurveyResponse.Reason))
            {
                options.CancellationDetails.Feedback = offboardingSurveyResponse.Reason;
            }

            await stripeAdapter.SubscriptionUpdateAsync(subscription.Id, options);
        }
    }

    public async Task<Customer> GetCustomer(
        ISubscriber subscriber,
        CustomerGetOptions customerGetOptions = null)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (string.IsNullOrEmpty(subscriber.GatewayCustomerId))
        {
            logger.LogError("Cannot retrieve customer for subscriber ({SubscriberID}) with no {FieldName}", subscriber.Id, nameof(subscriber.GatewayCustomerId));

            return null;
        }

        try
        {
            var customer = await stripeAdapter.CustomerGetAsync(subscriber.GatewayCustomerId, customerGetOptions);

            if (customer != null)
            {
                return customer;
            }

            logger.LogError("Could not find Stripe customer ({CustomerID}) for subscriber ({SubscriberID})",
                subscriber.GatewayCustomerId, subscriber.Id);

            return null;
        }
        catch (StripeException exception)
        {
            logger.LogError("An error occurred while trying to retrieve Stripe customer ({CustomerID}) for subscriber ({SubscriberID}): {Error}",
                subscriber.GatewayCustomerId, subscriber.Id, exception.Message);

            return null;
        }
    }

    public async Task<Customer> GetCustomerOrThrow(
        ISubscriber subscriber,
        CustomerGetOptions customerGetOptions = null)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (string.IsNullOrEmpty(subscriber.GatewayCustomerId))
        {
            logger.LogError("Cannot retrieve customer for subscriber ({SubscriberID}) with no {FieldName}", subscriber.Id, nameof(subscriber.GatewayCustomerId));

            throw ContactSupport();
        }

        try
        {
            var customer = await stripeAdapter.CustomerGetAsync(subscriber.GatewayCustomerId, customerGetOptions);

            if (customer != null)
            {
                return customer;
            }

            logger.LogError("Could not find Stripe customer ({CustomerID}) for subscriber ({SubscriberID})",
                subscriber.GatewayCustomerId, subscriber.Id);

            throw ContactSupport();
        }
        catch (StripeException exception)
        {
            logger.LogError("An error occurred while trying to retrieve Stripe customer ({CustomerID}) for subscriber ({SubscriberID}): {Error}",
                subscriber.GatewayCustomerId, subscriber.Id, exception.Message);

            throw ContactSupport("An error occurred while trying to retrieve a Stripe Customer", exception);
        }
    }

    public async Task<Subscription> GetSubscription(
        ISubscriber subscriber,
        SubscriptionGetOptions subscriptionGetOptions = null)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (string.IsNullOrEmpty(subscriber.GatewaySubscriptionId))
        {
            logger.LogError("Cannot retrieve subscription for subscriber ({SubscriberID}) with no {FieldName}", subscriber.Id, nameof(subscriber.GatewaySubscriptionId));

            return null;
        }

        try
        {
            var subscription = await stripeAdapter.SubscriptionGetAsync(subscriber.GatewaySubscriptionId, subscriptionGetOptions);

            if (subscription != null)
            {
                return subscription;
            }

            logger.LogError("Could not find Stripe subscription ({SubscriptionID}) for subscriber ({SubscriberID})",
                subscriber.GatewaySubscriptionId, subscriber.Id);

            return null;
        }
        catch (StripeException exception)
        {
            logger.LogError("An error occurred while trying to retrieve Stripe subscription ({SubscriptionID}) for subscriber ({SubscriberID}): {Error}",
                subscriber.GatewaySubscriptionId, subscriber.Id, exception.Message);

            return null;
        }
    }

    public async Task<Subscription> GetSubscriptionOrThrow(
        ISubscriber subscriber,
        SubscriptionGetOptions subscriptionGetOptions = null)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (string.IsNullOrEmpty(subscriber.GatewaySubscriptionId))
        {
            logger.LogError("Cannot retrieve subscription for subscriber ({SubscriberID}) with no {FieldName}", subscriber.Id, nameof(subscriber.GatewaySubscriptionId));

            throw ContactSupport();
        }

        try
        {
            var subscription = await stripeAdapter.SubscriptionGetAsync(subscriber.GatewaySubscriptionId, subscriptionGetOptions);

            if (subscription != null)
            {
                return subscription;
            }

            logger.LogError("Could not find Stripe subscription ({SubscriptionID}) for subscriber ({SubscriberID})",
                subscriber.GatewaySubscriptionId, subscriber.Id);

            throw ContactSupport();
        }
        catch (StripeException exception)
        {
            logger.LogError("An error occurred while trying to retrieve Stripe subscription ({SubscriptionID}) for subscriber ({SubscriberID}): {Error}",
                subscriber.GatewaySubscriptionId, subscriber.Id, exception.Message);

            throw ContactSupport("An error occurred while trying to retrieve a Stripe Subscription", exception);
        }
    }

    public async Task<TaxInformationDTO> GetTaxInformation(
        ISubscriber subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        var customer = await GetCustomerOrThrow(subscriber, new CustomerGetOptions { Expand = ["tax_ids"] });

        return GetTaxInformationDTOFrom(customer);
    }

    public async Task RemovePaymentMethod(
        ISubscriber subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        if (string.IsNullOrEmpty(subscriber.GatewayCustomerId))
        {
            throw ContactSupport();
        }

        var stripeCustomer = await GetCustomerOrThrow(subscriber, new CustomerGetOptions
        {
            Expand = ["invoice_settings.default_payment_method", "sources"]
        });

        if (stripeCustomer.Metadata?.TryGetValue(BraintreeCustomerIdKey, out var braintreeCustomerId) ?? false)
        {
            var braintreeCustomer = await braintreeGateway.Customer.FindAsync(braintreeCustomerId);

            if (braintreeCustomer == null)
            {
                logger.LogError("Failed to retrieve Braintree customer ({ID}) when removing payment method", braintreeCustomerId);

                throw ContactSupport();
            }

            if (braintreeCustomer.DefaultPaymentMethod != null)
            {
                var existingDefaultPaymentMethod = braintreeCustomer.DefaultPaymentMethod;

                var updateCustomerResult = await braintreeGateway.Customer.UpdateAsync(
                    braintreeCustomerId,
                    new CustomerRequest { DefaultPaymentMethodToken = null });

                if (!updateCustomerResult.IsSuccess())
                {
                    logger.LogError("Failed to update payment method for Braintree customer ({ID}) | Message: {Message}",
                        braintreeCustomerId, updateCustomerResult.Message);

                    throw ContactSupport();
                }

                var deletePaymentMethodResult = await braintreeGateway.PaymentMethod.DeleteAsync(existingDefaultPaymentMethod.Token);

                if (!deletePaymentMethodResult.IsSuccess())
                {
                    await braintreeGateway.Customer.UpdateAsync(
                        braintreeCustomerId,
                        new CustomerRequest { DefaultPaymentMethodToken = existingDefaultPaymentMethod.Token });

                    logger.LogError(
                        "Failed to delete Braintree payment method for Customer ({ID}), re-linked payment method. Message: {Message}",
                        braintreeCustomerId, deletePaymentMethodResult.Message);

                    throw ContactSupport();
                }
            }
            else
            {
                logger.LogWarning("Tried to remove non-existent Braintree payment method for Customer ({ID})", braintreeCustomerId);
            }
        }
        else
        {
            if (stripeCustomer.Sources != null && stripeCustomer.Sources.Any())
            {
                foreach (var source in stripeCustomer.Sources)
                {
                    switch (source)
                    {
                        case BankAccount:
                            await stripeAdapter.BankAccountDeleteAsync(stripeCustomer.Id, source.Id);
                            break;
                        case Card:
                            await stripeAdapter.CardDeleteAsync(stripeCustomer.Id, source.Id);
                            break;
                    }
                }
            }

            var paymentMethods = stripeAdapter.PaymentMethodListAutoPagingAsync(new PaymentMethodListOptions
            {
                Customer = stripeCustomer.Id
            });

            await foreach (var paymentMethod in paymentMethods)
            {
                await stripeAdapter.PaymentMethodDetachAsync(paymentMethod.Id);
            }
        }
    }

    public async Task UpdateTaxInformation(
        ISubscriber subscriber,
        TaxInformationDTO taxInformation)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        ArgumentNullException.ThrowIfNull(taxInformation);

        var customer = await GetCustomerOrThrow(subscriber, new CustomerGetOptions
        {
            Expand = ["tax_ids"]
        });

        await stripeAdapter.CustomerUpdateAsync(customer.Id, new CustomerUpdateOptions
        {
            Address = new AddressOptions
            {
                Country = taxInformation.Country,
                PostalCode = taxInformation.PostalCode,
                Line1 = taxInformation.Line1 ?? string.Empty,
                Line2 = taxInformation.Line2,
                City = taxInformation.City,
                State = taxInformation.State
            }
        });

        if (!subscriber.IsUser())
        {
            var taxId = customer.TaxIds?.FirstOrDefault();

            if (taxId != null)
            {
                await stripeAdapter.TaxIdDeleteAsync(customer.Id, taxId.Id);
            }

            var taxIdType = taxInformation.GetTaxIdType();

            if (!string.IsNullOrWhiteSpace(taxInformation.TaxId) &&
                !string.IsNullOrWhiteSpace(taxIdType))
            {
                await stripeAdapter.TaxIdCreateAsync(customer.Id, new TaxIdCreateOptions
                {
                    Type = taxIdType,
                    Value = taxInformation.TaxId,
                });
            }
        }
    }

    public async Task<BillingInfo.BillingSource> GetPaymentMethodAsync(ISubscriber subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        var customer = await GetCustomerOrThrow(subscriber, GetCustomerPaymentOptions());
        if (customer == null)
        {
            logger.LogError("Could not find Stripe customer ({CustomerID}) for subscriber ({SubscriberID})",
                subscriber.GatewayCustomerId, subscriber.Id);
            return null;
        }

        if (customer.Metadata?.ContainsKey("btCustomerId") ?? false)
        {
            try
            {
                var braintreeCustomer = await braintreeGateway.Customer.FindAsync(
                    customer.Metadata["btCustomerId"]);
                if (braintreeCustomer?.DefaultPaymentMethod != null)
                {
                    return new BillingInfo.BillingSource(
                        braintreeCustomer.DefaultPaymentMethod);
                }
            }
            catch (Braintree.Exceptions.NotFoundException ex)
            {
                logger.LogError("An error occurred while trying to retrieve braintree customer ({SubscriberID}): {Error}", subscriber.Id, ex.Message);
            }
        }

        if (customer.InvoiceSettings?.DefaultPaymentMethod?.Type == "card")
        {
            return new BillingInfo.BillingSource(
                customer.InvoiceSettings.DefaultPaymentMethod);
        }

        if (customer.DefaultSource != null &&
            (customer.DefaultSource is Card || customer.DefaultSource is BankAccount))
        {
            return new BillingInfo.BillingSource(customer.DefaultSource);
        }

        var paymentMethod = GetLatestCardPaymentMethod(customer.Id);
        return paymentMethod != null ? new BillingInfo.BillingSource(paymentMethod) : null;
    }

    private static CustomerGetOptions GetCustomerPaymentOptions()
    {
        var customerOptions = new CustomerGetOptions();
        customerOptions.AddExpand("default_source");
        customerOptions.AddExpand("invoice_settings.default_payment_method");
        return customerOptions;
    }

    private Stripe.PaymentMethod GetLatestCardPaymentMethod(string customerId)
    {
        var cardPaymentMethods = stripeAdapter.PaymentMethodListAutoPaging(
            new PaymentMethodListOptions { Customer = customerId, Type = "card" });
        return cardPaymentMethods.MaxBy(m => m.Created);
    }

    #region Shared Utilities

    private static TaxInformationDTO GetTaxInformationDTOFrom(
        Customer customer)
    {
        if (customer.Address == null)
        {
            return null;
        }

        return new TaxInformationDTO(
            customer.Address.Country,
            customer.Address.PostalCode,
            customer.TaxIds?.FirstOrDefault()?.Value,
            customer.Address.Line1,
            customer.Address.Line2,
            customer.Address.City,
            customer.Address.State);
    }

    #endregion
}
