﻿using System.Linq;
using IdentityServer4.Stores;
using System.Threading.Tasks;
using IdentityServer4.Models;
using System.Collections.Generic;
using Bit.Core.Repositories;
using System;
using IdentityModel;
using Bit.Core.Utilities;
using System.Security.Claims;
using Bit.Core.Services;

namespace Bit.Core.IdentityServer
{
    public class ClientStore : IClientStore
    {
        private readonly IInstallationRepository _installationRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IUserRepository _userRepository;
        private readonly GlobalSettings _globalSettings;
        private readonly StaticClientStore _staticClientStore;
        private readonly ILicensingService _licensingService;
        private readonly CurrentContext _currentContext;
        private readonly IOrganizationUserRepository _organizationUserRepository;

        public ClientStore(
            IInstallationRepository installationRepository,
            IOrganizationRepository organizationRepository,
            IUserRepository userRepository,
            GlobalSettings globalSettings,
            StaticClientStore staticClientStore,
            ILicensingService licensingService,
            CurrentContext currentContext,
            IOrganizationUserRepository organizationUserRepository)
        {
            _installationRepository = installationRepository;
            _organizationRepository = organizationRepository;
            _userRepository = userRepository;
            _globalSettings = globalSettings;
            _staticClientStore = staticClientStore;
            _licensingService = licensingService; 
            _currentContext = currentContext;
            _organizationUserRepository = organizationUserRepository;
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            if (!_globalSettings.SelfHosted && clientId.StartsWith("installation."))
            {
                var idParts = clientId.Split('.');
                if (idParts.Length > 1 && Guid.TryParse(idParts[1], out Guid id))
                {
                    var installation = await _installationRepository.GetByIdAsync(id);
                    if (installation != null)
                    {
                        return new Client
                        {
                            ClientId = $"installation.{installation.Id}",
                            RequireClientSecret = true,
                            ClientSecrets = { new Secret(installation.Key.Sha256()) },
                            AllowedScopes = new string[] { "api.push", "api.licensing" },
                            AllowedGrantTypes = GrantTypes.ClientCredentials,
                            AccessTokenLifetime = 3600 * 24,
                            Enabled = installation.Enabled,
                            Claims = new List<ClientClaim>
                            {
                                new ClientClaim(JwtClaimTypes.Subject, installation.Id.ToString())
                            }
                        };
                    }
                }
            }
            else if (_globalSettings.SelfHosted && clientId.StartsWith("internal.") &&
                CoreHelpers.SettingHasValue(_globalSettings.InternalIdentityKey))
            {
                var idParts = clientId.Split('.');
                if (idParts.Length > 1)
                {
                    var id = idParts[1];
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        return new Client
                        {
                            ClientId = $"internal.{id}",
                            RequireClientSecret = true,
                            ClientSecrets = { new Secret(_globalSettings.InternalIdentityKey.Sha256()) },
                            AllowedScopes = new string[] { "internal" },
                            AllowedGrantTypes = GrantTypes.ClientCredentials,
                            AccessTokenLifetime = 3600 * 24,
                            Enabled = true,
                            Claims = new List<ClientClaim>
                            {
                                new ClientClaim(JwtClaimTypes.Subject, id)
                            }
                        };
                    }
                }
            }
            else if (clientId.StartsWith("organization."))
            {
                var idParts = clientId.Split('.');
                if (idParts.Length > 1 && Guid.TryParse(idParts[1], out var id))
                {
                    var org = await _organizationRepository.GetByIdAsync(id);
                    if (org != null)
                    {
                        return new Client
                        {
                            ClientId = $"organization.{org.Id}",
                            RequireClientSecret = true,
                            ClientSecrets = { new Secret(org.ApiKey.Sha256()) },
                            AllowedScopes = new string[] { "api.organization" },
                            AllowedGrantTypes = GrantTypes.ClientCredentials,
                            AccessTokenLifetime = 3600 * 1,
                            Enabled = org.Enabled && org.UseApi,
                            Claims = new List<ClientClaim>
                            {
                                new ClientClaim(JwtClaimTypes.Subject, org.Id.ToString())
                            }
                        };
                    }
                }
            }
            else if (clientId.StartsWith("user."))
            {
                var idParts = clientId.Split('.');
                if (idParts.Length > 1 && Guid.TryParse(idParts[1], out var id))
                {
                    var user = await _userRepository.GetByIdAsync(id);
                    if (user != null)
                    {
                        var claims = new List<ClientClaim>();
                        claims.Add(new ClientClaim(JwtClaimTypes.Subject, user.Id.ToString()));
                        claims.Add(new ClientClaim(JwtClaimTypes.AuthenticationMethod, "Application", "external"));
                        var isPremium = await _licensingService.ValidateUserPremiumAsync(user);
                        claims.AddRange(new List<ClientClaim>
                        {
                            new ClientClaim("premium", isPremium ? "true" : "false", ClaimValueTypes.Boolean),
                            new ClientClaim(JwtClaimTypes.Email, user.Email),
                            new ClientClaim(JwtClaimTypes.EmailVerified, user.EmailVerified ? "true" : "false",
                                ClaimValueTypes.Boolean),
                            new ClientClaim("sstamp", user.SecurityStamp)
                        });

                        if (!string.IsNullOrWhiteSpace(user.Name))
                        {
                            claims.Add(new ClientClaim(JwtClaimTypes.Name, user.Name));
                        }

                        // Orgs that this user belongs to
                        var orgs = await _currentContext.OrganizationMembershipAsync(_organizationUserRepository, user.Id);
                        if (orgs.Any())
                        {
                            foreach (var group in orgs.GroupBy(o => o.Type))
                            {
                                switch (group.Key)
                                {
                                    case Enums.OrganizationUserType.Owner:
                                        foreach (var org in group)
                                        {
                                            claims.Add(new ClientClaim("orgowner", org.Id.ToString()));
                                        }
                                        break;
                                    case Enums.OrganizationUserType.Admin:
                                        foreach (var org in group)
                                        {
                                            claims.Add(new ClientClaim("orgadmin", org.Id.ToString()));
                                        }
                                        break;
                                    case Enums.OrganizationUserType.Manager:
                                        foreach (var org in group)
                                        {
                                            claims.Add(new ClientClaim("orgmanager", org.Id.ToString()));
                                        }
                                        break;
                                    case Enums.OrganizationUserType.User:
                                        foreach (var org in group)
                                        {
                                            claims.Add(new ClientClaim("orguser", org.Id.ToString()));
                                        }
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                        return new Client
                        {
                            ClientId = clientId,
                            RequireClientSecret = true,
                            ClientSecrets = { new Secret(user.ApiKey.Sha256()) },
                            AllowedScopes = new string[] { "api" },
                            AllowedGrantTypes = GrantTypes.ClientCredentials,
                            AccessTokenLifetime = 3600 * 1,
                            ClientClaimsPrefix = null,
                            Claims = claims
                        };
                    }
                }
            }

            return _staticClientStore.ApiClients.ContainsKey(clientId) ?
                _staticClientStore.ApiClients[clientId] : null;
        }
    }
}
