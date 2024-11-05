CREATE OR ALTER PROCEDURE [dbo].[OrganizationUser_ReadByOrganizationIdWithClaimedDomains]
    @OrganizationId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT OU.*
    FROM [dbo].[OrganizationUserView] OU
    INNER JOIN [dbo].[UserView] U ON OU.[UserId] = U.[Id]
    WHERE OU.[OrganizationId] = @OrganizationId
        AND (
            OU.[Status] = 2 -- Confirmed
            OR OU.[Status] = -1 -- Revoked
        )
        AND EXISTS (
            SELECT 1
            FROM [dbo].[OrganizationDomainView] OD
            WHERE OD.[OrganizationId] = @OrganizationId
                AND OD.[VerifiedDate] IS NOT NULL
                AND U.[Email] LIKE '%@' + OD.[DomainName]
        );
END
GO

CREATE OR ALTER PROCEDURE [dbo].[Organization_ReadByClaimedUserEmailDomain]
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT O.*
    FROM [dbo].[UserView] U
    INNER JOIN [dbo].[OrganizationUserView] OU ON U.[Id] = OU.[UserId]
    INNER JOIN [dbo].[OrganizationView] O ON OU.[OrganizationId] = O.[Id]
    INNER JOIN [dbo].[OrganizationDomainView] OD ON OU.[OrganizationId] = OD.[OrganizationId]
    WHERE U.[Id] = @UserId
        AND (
            OU.[Status] = 2 -- Confirmed
            OR OU.[Status] = -1 -- Revoked
        )
        AND OD.[VerifiedDate] IS NOT NULL
        AND U.[Email] LIKE '%@' + OD.[DomainName];
END
GO
