﻿using Bit.Core.AdminConsole.Entities;
using Bit.Scim.Models;

namespace Bit.Scim.Groups.Interfaces;

public interface IPostGroupCommand
{
    Task<Group> PostGroupAsync(Organization organization, ScimGroupRequestModel model);
}
