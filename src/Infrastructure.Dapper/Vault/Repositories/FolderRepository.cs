﻿using System.Data;
using Bit.Core.Auth.UserFeatures.UserKey;
using Bit.Core.Settings;
using Bit.Core.Vault.Entities;
using Bit.Core.Vault.Repositories;
using Bit.Infrastructure.Dapper.Repositories;
using Bit.Infrastructure.Dapper.Vault.Helpers;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bit.Infrastructure.Dapper.Vault.Repositories;

public class FolderRepository : Repository<Folder, Guid>, IFolderRepository
{
    public FolderRepository(GlobalSettings globalSettings)
        : this(globalSettings.SqlServer.ConnectionString, globalSettings.SqlServer.ReadOnlyConnectionString)
    { }

    public FolderRepository(string connectionString, string readOnlyConnectionString)
        : base(connectionString, readOnlyConnectionString)
    { }

    public async Task<Folder> GetByIdAsync(Guid id, Guid userId)
    {
        var folder = await GetByIdAsync(id);
        if (folder == null || folder.UserId != userId)
        {
            return null;
        }

        return folder;
    }

    public async Task<ICollection<Folder>> GetManyByUserIdAsync(Guid userId)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            var results = await connection.QueryAsync<Folder>(
                $"[{Schema}].[Folder_ReadByUserId]",
                new { UserId = userId },
                commandType: CommandType.StoredProcedure);

            return results.ToList();
        }
    }

    public override Task DeleteAsync(Folder folder)
    {
        throw new NotImplementedException("Use the overload for FolderRepository.DeleteAsync instead.");
    }

    public async Task DeleteAsync(Folder folder, bool useFlexibleCollections)
    {
        var sprocName = useFlexibleCollections
                ? $"[{Schema}].[Folder_DeleteById_V2]"
                : $"[{Schema}].[Folder_DeleteById]";

        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.ExecuteAsync(
                sprocName,
                new { Id = folder.Id },
                commandType: CommandType.StoredProcedure);
        }
    }

    /// <inheritdoc />
    public UpdateEncryptedDataForKeyRotation UpdateForKeyRotation(
        Guid userId, IEnumerable<Folder> folders)
    {
        return async (SqlConnection connection, SqlTransaction transaction) =>
        {
            // Create temp table
            var sqlCreateTemp = @"
                            SELECT TOP 0 *
                            INTO #TempFolder
                            FROM [dbo].[Folder]";

            await using (var cmd = new SqlCommand(sqlCreateTemp, connection, transaction))
            {
                cmd.ExecuteNonQuery();
            }

            // Bulk copy data into temp table
            using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction))
            {
                bulkCopy.DestinationTableName = "#TempFolder";
                var foldersTable = folders.ToDataTable();
                foreach (DataColumn col in foldersTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                }

                foldersTable.PrimaryKey = new DataColumn[] { foldersTable.Columns[0] };
                await bulkCopy.WriteToServerAsync(foldersTable);
            }

            // Update folder table from temp table
            var sql = @"
                    UPDATE
                        [dbo].[Folder]
                    SET
                        [Name] = TF.[Name],
                        [RevisionDate] = TF.[RevisionDate]
                    FROM
                        [dbo].[Folder] F
                    INNER JOIN
                        #TempFolder TF ON F.Id = TF.Id
                    WHERE
                        F.[UserId] = @UserId;

                    DROP TABLE #TempFolder";

            await using (var cmd = new SqlCommand(sql, connection, transaction))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.UniqueIdentifier).Value = userId;
                cmd.ExecuteNonQuery();
            }
        };
    }

}
