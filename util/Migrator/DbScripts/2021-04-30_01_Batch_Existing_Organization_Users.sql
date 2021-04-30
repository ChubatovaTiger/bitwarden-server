IF OBJECT_ID('[dbo].[OrganizationUser_ReadExistingByOrganizationIdEmail]') IS NOT NULL
BEGIN
    DROP PROCEDURE [dbo].[OrganizationUser_ReadExistingByOrganizationIdEmail]
END
GO

-- Create sproc to return existing users
CREATE PROCEDURE [dbo].[OrganizationUser_ReadExistingByOrganizationIdEmail]
    @OrganizationId UNIQUEIDENTIFIER,
    @Emails [dbo].[EmailArray] READONLY,
    @OnlyUsers BIT
AS
BEGIN
    SET NOCOUNT ON

    SELECT
        E.Email
    FROM
        @Emails E
        INNER JOIN
        (
            SELECT
                U.[Email] as 'UEmail',
                OU.[Email] as 'OUEmail',
                OU.OrganizationId
            FROM
                [dbo].[User] U
            RIGHT JOIN
                [dbo].[OrganizationUser] OU ON OU.[UserId] = U.[Id]
            WHERE
                OU.OrganizationId = @OrganizationId
        ) OUU ON OUU.[UEmail] = E.[Email] OR OUU.[OUEmail] = E.[Email]
    WHERE
        (@OnlyUsers = 0 AND (OUU.UEmail IS NOT NULL OR OUU.OUEmail IS NOT NULL)) OR
        (@OnlyUsers = 1 AND (OUU.UEmail IS NOT NULL))
    
END
GO
