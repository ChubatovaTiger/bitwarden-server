﻿using Bit.Core.Enums;
using Bit.Core.Models.Data;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Utilities;
using Bit.Scim.Context;
using Bit.Scim.Models;
using Bit.Scim.Users.Interfaces;
using Bit.Scim.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Scim.Controllers.v2;

[Authorize("Scim")]
[Route("v2/{organizationId}/users")]
[ExceptionHandlerFilter]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationService _organizationService;
    private readonly IScimContext _scimContext;
    private readonly IGetUserQuery _getUserQuery;
    private readonly IGetUsersListQuery _getUsersListQuery;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationService organizationService,
        IScimContext scimContext,
        IGetUserQuery getUserQuery,
        IGetUsersListQuery getUsersListQuery,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _organizationUserRepository = organizationUserRepository;
        _organizationService = organizationService;
        _scimContext = scimContext;
        _getUserQuery = getUserQuery;
        _getUsersListQuery = getUsersListQuery;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid id)
    {
        var orgUser = await _getUserQuery.GetUserAsync(organizationId, id);
        var scimUserResponseModel = new ScimUserResponseModel(orgUser);
        return Ok(scimUserResponseModel);
    }

    [HttpGet("")]
    public async Task<IActionResult> Get(
        Guid organizationId,
        [FromQuery] string filter,
        [FromQuery] int? count,
        [FromQuery] int? startIndex)
    {
        var usersListQueryResult = await _getUsersListQuery.GetUsersListAsync(organizationId, filter, count, startIndex);
        var scimListResponseModel = new ScimListResponseModel<ScimUserResponseModel>
        {
            Resources = usersListQueryResult.userList.Select(u => new ScimUserResponseModel(u)).ToList(),
            ItemsPerPage = count.GetValueOrDefault(usersListQueryResult.userList.Count()),
            TotalResults = usersListQueryResult.totalResults,
            StartIndex = startIndex.GetValueOrDefault(1),
        };
        return Ok(scimListResponseModel);
    }

    [HttpPost("")]
    public async Task<IActionResult> Post(Guid organizationId, [FromBody] ScimUserRequestModel model)
    {
        var email = model.PrimaryEmail?.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            switch (_scimContext.RequestScimProvider)
            {
                case ScimProviderType.AzureAd:
                    email = model.UserName?.ToLowerInvariant();
                    break;
                default:
                    email = model.WorkEmail?.ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        email = model.Emails?.FirstOrDefault()?.Value?.ToLowerInvariant();
                    }
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(email) || !model.Active)
        {
            return new BadRequestResult();
        }

        var orgUsers = await _organizationUserRepository.GetManyDetailsByOrganizationAsync(organizationId);
        var orgUserByEmail = orgUsers.FirstOrDefault(ou => ou.Email?.ToLowerInvariant() == email);
        if (orgUserByEmail != null)
        {
            return new ConflictResult();
        }

        string externalId = null;
        if (!string.IsNullOrWhiteSpace(model.ExternalId))
        {
            externalId = model.ExternalId;
        }
        else if (!string.IsNullOrWhiteSpace(model.UserName))
        {
            externalId = model.UserName;
        }
        else
        {
            externalId = CoreHelpers.RandomString(15);
        }

        var orgUserByExternalId = orgUsers.FirstOrDefault(ou => ou.ExternalId == externalId);
        if (orgUserByExternalId != null)
        {
            return new ConflictResult();
        }

        var invitedOrgUser = await _organizationService.InviteUserAsync(organizationId, null, email,
            OrganizationUserType.User, false, externalId, new List<SelectionReadOnly>());
        var orgUser = await _organizationUserRepository.GetDetailsByIdAsync(invitedOrgUser.Id);
        var response = new ScimUserResponseModel(orgUser);
        return new CreatedResult(Url.Action(nameof(Get), new { orgUser.OrganizationId, orgUser.Id }), response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(Guid organizationId, Guid id, [FromBody] ScimUserRequestModel model)
    {
        var orgUser = await _organizationUserRepository.GetByIdAsync(id);
        if (orgUser == null || orgUser.OrganizationId != organizationId)
        {
            return new NotFoundObjectResult(new ScimErrorResponseModel
            {
                Status = 404,
                Detail = "User not found."
            });
        }

        if (model.Active && orgUser.Status == OrganizationUserStatusType.Revoked)
        {
            await _organizationService.RestoreUserAsync(orgUser, null, _userService);
        }
        else if (!model.Active && orgUser.Status != OrganizationUserStatusType.Revoked)
        {
            await _organizationService.RevokeUserAsync(orgUser, null);
        }

        // Have to get full details object for response model
        var orgUserDetails = await _organizationUserRepository.GetDetailsByIdAsync(id);
        return new ObjectResult(new ScimUserResponseModel(orgUserDetails));
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Patch(Guid organizationId, Guid id, [FromBody] ScimPatchModel model)
    {
        var orgUser = await _organizationUserRepository.GetByIdAsync(id);
        if (orgUser == null || orgUser.OrganizationId != organizationId)
        {
            return new NotFoundObjectResult(new ScimErrorResponseModel
            {
                Status = 404,
                Detail = "User not found."
            });
        }

        var operationHandled = false;
        foreach (var operation in model.Operations)
        {
            // Replace operations
            if (operation.Op?.ToLowerInvariant() == "replace")
            {
                // Active from path
                if (operation.Path?.ToLowerInvariant() == "active")
                {
                    var active = operation.Value.ToString()?.ToLowerInvariant();
                    var handled = await HandleActiveOperationAsync(orgUser, active == "true");
                    if (!operationHandled)
                    {
                        operationHandled = handled;
                    }
                }
                // Active from value object
                else if (string.IsNullOrWhiteSpace(operation.Path) &&
                    operation.Value.TryGetProperty("active", out var activeProperty))
                {
                    var handled = await HandleActiveOperationAsync(orgUser, activeProperty.GetBoolean());
                    if (!operationHandled)
                    {
                        operationHandled = handled;
                    }
                }
            }
        }

        if (!operationHandled)
        {
            _logger.LogWarning("User patch operation not handled: {operation} : ",
                string.Join(", ", model.Operations.Select(o => $"{o.Op}:{o.Path}")));
        }

        return new NoContentResult();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        var orgUser = await _organizationUserRepository.GetByIdAsync(id);
        if (orgUser == null || orgUser.OrganizationId != organizationId)
        {
            return new NotFoundObjectResult(new ScimErrorResponseModel
            {
                Status = 404,
                Detail = "User not found."
            });
        }
        await _organizationService.DeleteUserAsync(organizationId, id, null);
        return new NoContentResult();
    }

    private async Task<bool> HandleActiveOperationAsync(Core.Entities.OrganizationUser orgUser, bool active)
    {
        if (active && orgUser.Status == OrganizationUserStatusType.Revoked)
        {
            await _organizationService.RestoreUserAsync(orgUser, null, _userService);
            return true;
        }
        else if (!active && orgUser.Status != OrganizationUserStatusType.Revoked)
        {
            await _organizationService.RevokeUserAsync(orgUser, null);
            return true;
        }
        return false;
    }
}
