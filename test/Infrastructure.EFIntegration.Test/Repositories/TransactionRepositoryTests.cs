﻿using Bit.Core.AdminConsole.Entities;
using Bit.Core.Entities;
using Bit.Core.Test.AutoFixture.Attributes;
using Bit.Infrastructure.EFIntegration.Test.AutoFixture;
using Bit.Infrastructure.EFIntegration.Test.Repositories.EqualityComparers;
using Xunit;
using EfAdminConsoleRepo = Bit.Infrastructure.EntityFramework.AdminConsole.Repositories;
using EfRepo = Bit.Infrastructure.EntityFramework.Repositories;
using SqlAdminConsoleRepo = Bit.Infrastructure.Dapper.AdminConsole.Repositories;
using SqlRepo = Bit.Infrastructure.Dapper.Repositories;

namespace Bit.Infrastructure.EFIntegration.Test.Repositories;

public class TransactionRepositoryTests
{

    [CiSkippedTheory, EfUserTransactionAutoData, EfOrganizationTransactionAutoData]
    public async void CreateAsync_Works_DataMatches(
        Transaction transaction,
        User user,
        Organization org,
        TransactionCompare equalityComparer,
        List<EfRepo.TransactionRepository> suts,
        List<EfRepo.UserRepository> efUserRepos,
        List<EfAdminConsoleRepo.OrganizationRepository> efOrgRepos,
        SqlRepo.TransactionRepository sqlTransactionRepo,
        SqlRepo.UserRepository sqlUserRepo,
        SqlAdminConsoleRepo.OrganizationRepository sqlOrgRepo
        )
    {
        var savedTransactions = new List<Transaction>();
        foreach (var sut in suts)
        {
            var i = suts.IndexOf(sut);
            var efUser = await efUserRepos[i].CreateAsync(user);
            if (transaction.OrganizationId.HasValue)
            {
                var efOrg = await efOrgRepos[i].CreateAsync(org);
                transaction.OrganizationId = efOrg.Id;
            }
            sut.ClearChangeTracking();

            transaction.UserId = efUser.Id;
            var postEfTransaction = await sut.CreateAsync(transaction);
            sut.ClearChangeTracking();

            var savedTransaction = await sut.GetByIdAsync(postEfTransaction.Id);
            savedTransactions.Add(savedTransaction);
        }

        var sqlUser = await sqlUserRepo.CreateAsync(user);
        if (transaction.OrganizationId.HasValue)
        {
            var sqlOrg = await sqlOrgRepo.CreateAsync(org);
            transaction.OrganizationId = sqlOrg.Id;
        }

        transaction.UserId = sqlUser.Id;
        var sqlTransaction = await sqlTransactionRepo.CreateAsync(transaction);
        var savedSqlTransaction = await sqlTransactionRepo.GetByIdAsync(sqlTransaction.Id);
        savedTransactions.Add(savedSqlTransaction);

        var distinctItems = savedTransactions.Distinct(equalityComparer);
        Assert.True(!distinctItems.Skip(1).Any());
    }
}
