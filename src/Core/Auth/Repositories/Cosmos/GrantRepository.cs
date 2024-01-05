﻿using System.Net;
using Bit.Core.Auth.Models.Data;
using Bit.Core.Settings;
using Microsoft.Azure.Cosmos;

namespace Bit.Core.Auth.Repositories.Cosmos;

public class GrantRepository : IGrantRepository
{
    private readonly CosmosClient _client;
    private readonly Database _database;
    private readonly Container _container;

    public GrantRepository(GlobalSettings globalSettings)
        : this(globalSettings.IdentityServer.CosmosConnectionString)
    { }

    public GrantRepository(string cosmosConnectionString)
    {
        var options = new CosmosClientOptions
        {
            SerializerOptions = new CosmosSerializationOptions
            {
                IgnoreNullValues = true,
                Indented = false
            }
        };
        _client = new CosmosClient(cosmosConnectionString, options);
        _database = _client.GetDatabase("identity");
        _container = _database.GetContainer("grant");
    }

    public async Task<IGrant> GetByKeyAsync(string key)
    {
        try
        {
            var response = await _container.ReadItemAsync<GrantItem>(key, new PartitionKey(key));
            return response.Resource;
        }
        catch (CosmosException e)
        {
            if (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw;
        }
    }

    public Task<ICollection<IGrant>> GetManyAsync(string subjectId, string sessionId, string clientId, string type) => throw new NotImplementedException();

    public async Task SaveAsync(IGrant obj)
    {
        if (obj is not GrantItem item)
        {
            item = new GrantItem(obj);
        }
        item.SetTtl();
        await _container.UpsertItemAsync(item, new PartitionKey(item.Key));
    }

    public async Task DeleteByKeyAsync(string key)
    {
        await _container.DeleteItemAsync<IGrant>(key, new PartitionKey(key));
    }

    public Task DeleteManyAsync(string subjectId, string sessionId, string clientId, string type) => throw new NotImplementedException();
}
