﻿using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Scim.Commands.Users;
using Bit.Scim.Models;
using MediatR;

namespace Bit.Scim.Handlers.Users
{
    public class PutUserHandler : IRequestHandler<PutUserCommand, ScimUserResponseModel>
    {
        private readonly IUserService _userService;
        private readonly IOrganizationUserRepository _organizationUserRepository;
        private readonly IOrganizationService _organizationService;

        public PutUserHandler(
            IUserService userService,
            IOrganizationUserRepository organizationUserRepository,
            IOrganizationService organizationService)
        {
            _userService = userService;
            _organizationUserRepository = organizationUserRepository;
            _organizationService = organizationService;
        }

        public async Task<ScimUserResponseModel> Handle(PutUserCommand request, CancellationToken cancellationToken)
        {
            var orgUser = await _organizationUserRepository.GetByIdAsync(request.Id);
            if (orgUser == null || orgUser.OrganizationId != request.OrganizationId)
            {
                throw new NotFoundException("User not found.");
            }

            if (request.Model.Active && orgUser.Status == OrganizationUserStatusType.Revoked)
            {
                await _organizationService.RestoreUserAsync(orgUser, null, _userService);
            }
            else if (!request.Model.Active && orgUser.Status != OrganizationUserStatusType.Revoked)
            {
                await _organizationService.RevokeUserAsync(orgUser, null);
            }

            // Have to get full details object for response model
            var orgUserDetails = await _organizationUserRepository.GetDetailsByIdAsync(request.Id);
            return new ScimUserResponseModel(orgUserDetails);
        }
    }
}
