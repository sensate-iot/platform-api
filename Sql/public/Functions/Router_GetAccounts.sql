
CREATE FUNCTION router_getaccounts()
    RETURNS TABLE(id UUID, billinglockout BOOLEAN, banned BOOLEAN)
    LANGUAGE plpgsql
AS
$$
BEGIN
RETURN query
    SELECT "Users"."Id"::uuid,
           "BillingLockout",
           "Roles"."NormalizedName" = 'BANNED' AS "Banned"
    FROM "Users"
    INNER JOIN "UserRoles" ON "Users"."Id" = "UserRoles"."UserId"
    INNER JOIN "Roles" ON "UserRoles"."RoleId" = "Roles"."Id"
    GROUP BY "Users"."Id", "BillingLockout", "Roles"."NormalizedName" = 'BANNED';
END
$$;
