-- Create Auth Request table
IF OBJECT_ID('[dbo].[AuthRequest]') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[AuthRequest]
END

IF OBJECT_ID('[dbo].[AuthRequest]') IS NULL
BEGIN
CREATE TABLE [dbo].[AuthRequest] (
    [Id]                        UNIQUEIDENTIFIER NOT NULL,
    [UserId]                    UNIQUEIDENTIFIER NOT NULL,
    [Type]                      SMALLINT         NOT NULL,
    [RequestDeviceIdentifier]   NVARCHAR(50)     NOT NULL,
    [RequestDeviceType]         SMALLINT         NOT NULL,
    [RequestIpAddress]          VARCHAR(50)      NOT NULL,
    [RequestFingerprint]        VARCHAR(MAX)     NOT NULL,
    [ResponseDeviceId]          UNIQUEIDENTIFIER NULL,
    [AccessCode]                VARCHAR(25)      NOT NULL,
    [PublicKey]                 VARCHAR(MAX)     NOT NULL,
    [Key]                       VARCHAR(MAX)     NULL,
    [MasterPasswordHash]        VARCHAR(MAX)     NULL,
    [CreationDate]              DATETIME2 (7)    NOT NULL,
    [ExpirationDate]            DATETIME2 (7)    NOT NULL,
    [ResponseDate]              DATETIME2 (7)    NULL,
    [AuthenticationDate]        DATETIME2 (7)    NULL,
    [FailedLoginAttempts]       TINYINT          NOT NULL,
    CONSTRAINT [PK_AuthRequest] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AuthRequest_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([Id]),
    CONSTRAINT [FK_AuthRequest_ResponseDevice] FOREIGN KEY ([ResponseDeviceId]) REFERENCES [dbo].[Device] ([Id])
);
END
GO

-- Create View
IF EXISTS(SELECT * FROM sys.views WHERE [Name] = 'AuthRequestView')
BEGIN
    DROP VIEW [dbo].[AuthRequestView]
END
GO

CREATE VIEW [dbo].[AuthRequestView]
AS
SELECT
    *
FROM
    [dbo].[AuthRequest]
GO

-- Auth Request CRUD sprocs
IF OBJECT_ID('[dbo].[AuthRequest_Create]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[AuthRequest_Create]
END
GO

CREATE PROCEDURE [dbo].[AuthRequest_Create]
    @Id UNIQUEIDENTIFIER OUTPUT,
    @UserId UNIQUEIDENTIFIER,
    @Type TINYINT,
    @RequestDeviceIdentifier NVARCHAR(50),
    @RequestDeviceType TINYINT,
    @RequestIpAddress VARCHAR(50),
    @RequestFingerprint VARCHAR(MAX),
    @ResponseDeviceId UNIQUEIDENTIFIER,
    @AccessCode VARCHAR(25),
    @PublicKey VARCHAR(MAX),
    @Key VARCHAR(MAX),
    @MasterPasswordHash VARCHAR(MAX),
    @CreationDate DATETIME2(7),
    @ExpirationDate DATETIME2(7),
    @ResponseDate DATETIME2(7),
    @AuthenticationDate DATETIME2(7),
    @FailedLoginAttempts TINYINT
AS
BEGIN
    SET NOCOUNT ON

    INSERT INTO [dbo].[AuthRequest]
    (
        [Id],
        [UserId],
        [Type],
        [RequestDeviceIdentifier],
        [RequestDeviceType],
        [RequestIpAddress],
        [RequestFingerprint],
        [ResponseDeviceId],
        [AccessCode],
        [PublicKey],
        [Key],
        [MasterPasswordHash],
        [CreationDate],
        [ExpirationDate],
        [ResponseDate],
        [AuthenticationDate],
        [FailedLoginAttempts]
    )
    VALUES
    (
        @Id,
        @UserId,
        @Type,
        @RequestDeviceIdentifier,
        @RequestDeviceType,
        @RequestIpAddress,
        @RequestFingerprint,
        @ResponseDeviceId,
        @AccessCode,
        @PublicKey,
        @Key,
        @MasterPasswordHash,
        @CreationDate,
        @ExpirationDate,
        @ResponseDate,
        @AuthenticationDate,
        @FailedLoginAttempts
    )
END
GO

IF OBJECT_ID('[dbo].[AuthRequest_Update]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[AuthRequest_Update]
END
GO

CREATE PROCEDURE [dbo].[AuthRequest_Update]
    @Id UNIQUEIDENTIFIER OUTPUT,
    @ResponseDeviceId UNIQUEIDENTIFIER,
    @Key VARCHAR(MAX),
    @MasterPasswordHash VARCHAR(MAX),
    @ResponseDate DATETIME2(7),
    @AuthenticationDate DATETIME2(7),
    @FailedLoginAttempts TINYINT
AS
BEGIN
    SET NOCOUNT ON

    UPDATE
        [dbo].[AuthRequest]
    SET
        [ResponseDeviceId] = @ResponseDeviceId,
        [Key] = @Key,
        [MasterPasswordHash] = @MasterPasswordHash,
        [ResponseDate] = @ResponseDate,
        [AuthenticationDate] = @AuthenticationDate,
        [FailedLoginAttempts] = @FailedLoginAttempts
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[AuthRequest_ReadById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[AuthRequest_ReadById]
END
GO

CREATE PROCEDURE [dbo].[AuthRequest_ReadById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        *
    FROM
        [dbo].[AuthRequestView]
    WHERE
        [Id] = @Id
END
GO

IF OBJECT_ID('[dbo].[AuthRequest_DeleteById]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[AuthRequest_DeleteById]
END
GO

CREATE PROCEDURE [dbo].[AuthRequest_DeleteById]
    @Id UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON

    DELETE
    FROM
        [dbo].[AuthRequest]
    WHERE
        [Id] = @Id
END
GO
