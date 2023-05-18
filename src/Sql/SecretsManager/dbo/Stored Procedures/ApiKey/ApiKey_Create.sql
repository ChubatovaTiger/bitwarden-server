CREATE PROCEDURE [dbo].[ApiKey_Create]
    @Id UNIQUEIDENTIFIER OUTPUT,
    @ServiceAccountId UNIQUEIDENTIFIER,
    @Name VARCHAR(200),
    @ClientSecret VARCHAR(30) = 'migrated', -- Deprecated as of 2023-05-17
    @HashedClientSecret VARCHAR(128) = NULL,
    @Scope NVARCHAR(4000),
    @EncryptedPayload NVARCHAR(4000),
    @Key VARCHAR(MAX),
    @ExpireAt DATETIME2(7),
    @CreationDate DATETIME2(7),
    @RevisionDate DATETIME2(7)
AS
BEGIN
    SET NOCOUNT ON

    IF (@HashedClientSecret IS NULL)
    BEGIN
      DECLARE @hb VARBINARY(128) = HASHBYTES('SHA2_256', @ClientSecret);
      SET @HashedClientSecret = CAST(N'' as xml).value('xs:base64Binary(sql:variable("@hb"))', 'varchar(128)');
    END

    INSERT INTO [dbo].[ApiKey] 
    (
        [Id],
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
    )
    VALUES 
    (
        @Id,
        @ServiceAccountId,
        @Name,
        @ClientSecret,
        @HashedClientSecret,
        @Scope,
        @EncryptedPayload,
        @Key,
        @ExpireAt,
        @CreationDate,
        @RevisionDate
    )
END
