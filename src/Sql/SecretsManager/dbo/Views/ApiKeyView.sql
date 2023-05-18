﻿CREATE VIEW [dbo].[ApiKeyView]
AS
SELECT [Id],
    [ServiceAccountId],
    [Name],
    [ClientSecret],
    [HashedClientSecret],
    [Scope],
    [EncryptedPayload],
    [Key],
    [ExpireAt],
    [CreationDate],
    [RevisionDate]
FROM [dbo].[ApiKey]
