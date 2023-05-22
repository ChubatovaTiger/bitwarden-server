﻿using AutoMapper;
using Bit.Infrastructure.EntityFramework.Models;

namespace Bit.Infrastructure.EntityFramework.Vault.Models;

public class Folder : Core.Vault.Entities.Folder
{
    public virtual User User { get; set; }
}

public class FolderMapperProfile : Profile
{
    public FolderMapperProfile()
    {
        CreateMap<Core.Vault.Entities.Folder, Folder>().ReverseMap();
    }
}
