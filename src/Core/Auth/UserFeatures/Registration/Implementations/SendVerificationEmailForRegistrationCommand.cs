﻿#nullable enable
using Bit.Core.Auth.Models.Business.Tokenables;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.Core.Tokens;

namespace Bit.Core.Auth.UserFeatures.Registration.Implementations;

public class SendVerificationEmailForRegistrationCommand : ISendVerificationEmailForRegistrationCommand
{

    private readonly IUserRepository _userRepository;
    private readonly GlobalSettings _globalSettings;
    private readonly IMailService _mailService;
    private readonly IDataProtectorTokenFactory<RegistrationEmailVerificationTokenable> _tokenDataFactory;

    public SendVerificationEmailForRegistrationCommand(
        IUserRepository userRepository,
        GlobalSettings globalSettings,
        IMailService mailService,
        IDataProtectorTokenFactory<RegistrationEmailVerificationTokenable> tokenDataFactory)
    {
        _userRepository = userRepository;
        _globalSettings = globalSettings;
        _mailService = mailService;
        _tokenDataFactory = tokenDataFactory;
    }

    public async Task<string?> Run(string email, string? name, bool receiveMarketingEmails)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentNullException(nameof(email));
        }

        // Check to see if the user already exists
        var user = await _userRepository.GetByEmailAsync(email);
        var userExists = user != null;

        if (!_globalSettings.EnableEmailVerification)
        {

            if (userExists)
            {
                // Add delay to prevent timing attacks
                await Task.Delay(130);
                throw new BadRequestException($"Email {email} is already taken");
            }

            // if user doesn't exist, return a EmailVerificationTokenable in the response body.
            var token = GenerateToken(email, name, receiveMarketingEmails);

            return token;
        }

        if (!userExists)
        {
            // If the user doesn't exist, create a new EmailVerificationTokenable and send the user
            // an email with a link to verify their email address
            var token = GenerateToken(email, name, receiveMarketingEmails);
            await _mailService.SendRegistrationVerificationEmailAsync(email, token);
        }

        // Add delay to prevent timing attacks
        await Task.Delay(130);
        // User exists but we will return a 200 regardless of whether the email was sent or not; so return null
        return null;
    }

    private string GenerateToken(string email, string? name, bool receiveMarketingEmails)
    {
        var registrationEmailVerificationTokenable = new RegistrationEmailVerificationTokenable(email, name, receiveMarketingEmails);
        return _tokenDataFactory.Protect(registrationEmailVerificationTokenable);
    }
}

