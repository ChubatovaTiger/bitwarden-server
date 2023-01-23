﻿using Bit.Api.Models.Response;
using Bit.Api.SecretsManager.Models.Request;
using Bit.Api.SecretsManager.Models.Response;
using Bit.Core.SecretsManager.Commands.AccessTokens.Interfaces;
using Bit.Core.SecretsManager.Commands.ServiceAccounts.Interfaces;
using Bit.Core.SecretsManager.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.SecretsManager.Controllers;

[SecretsManager]
[Route("service-accounts")]
public class ServiceAccountsController : Controller
{
    private readonly IServiceAccountRepository _serviceAccountRepository;
    private readonly IApiKeyRepository _apiKeyRepository;
    private readonly ICreateServiceAccountCommand _createServiceAccountCommand;
    private readonly ICreateAccessTokenCommand _createAccessTokenCommand;
    private readonly IUpdateServiceAccountCommand _updateServiceAccountCommand;

    public ServiceAccountsController(
        IServiceAccountRepository serviceAccountRepository,
        ICreateAccessTokenCommand createAccessTokenCommand,
        IApiKeyRepository apiKeyRepository, ICreateServiceAccountCommand createServiceAccountCommand,
        IUpdateServiceAccountCommand updateServiceAccountCommand)
    {
        _serviceAccountRepository = serviceAccountRepository;
        _apiKeyRepository = apiKeyRepository;
        _createServiceAccountCommand = createServiceAccountCommand;
        _updateServiceAccountCommand = updateServiceAccountCommand;
        _createAccessTokenCommand = createAccessTokenCommand;
    }

    [HttpGet("/organizations/{organizationId}/service-accounts")]
    public async Task<ListResponseModel<ServiceAccountResponseModel>> GetServiceAccountsByOrganizationAsync([FromRoute] Guid organizationId)
    {
        var serviceAccounts = await _serviceAccountRepository.GetManyByOrganizationIdAsync(organizationId);
        var responses = serviceAccounts.Select(serviceAccount => new ServiceAccountResponseModel(serviceAccount));
        return new ListResponseModel<ServiceAccountResponseModel>(responses);
    }

    [HttpPost("/organizations/{organizationId}/service-accounts")]
    public async Task<ServiceAccountResponseModel> CreateServiceAccountAsync([FromRoute] Guid organizationId, [FromBody] ServiceAccountCreateRequestModel createRequest)
    {
        var result = await _createServiceAccountCommand.CreateAsync(createRequest.ToServiceAccount(organizationId));
        return new ServiceAccountResponseModel(result);
    }

    [HttpPut("{id}")]
    public async Task<ServiceAccountResponseModel> UpdateServiceAccountAsync([FromRoute] Guid id, [FromBody] ServiceAccountUpdateRequestModel updateRequest)
    {
        var result = await _updateServiceAccountCommand.UpdateAsync(updateRequest.ToServiceAccount(id));
        return new ServiceAccountResponseModel(result);
    }

    [HttpGet("{id}/access-tokens")]
    public async Task<ListResponseModel<AccessTokenResponseModel>> GetAccessTokens([FromRoute] Guid id)
    {
        var accessTokens = await _apiKeyRepository.GetManyByServiceAccountIdAsync(id);
        var responses = accessTokens.Select(token => new AccessTokenResponseModel(token));
        return new ListResponseModel<AccessTokenResponseModel>(responses);
    }

    [HttpPost("{id}/access-tokens")]
    public async Task<AccessTokenCreationResponseModel> CreateAccessTokenAsync([FromRoute] Guid id, [FromBody] AccessTokenCreateRequestModel request)
    {
        var result = await _createAccessTokenCommand.CreateAsync(request.ToApiKey(id));
        return new AccessTokenCreationResponseModel(result);
    }
}
