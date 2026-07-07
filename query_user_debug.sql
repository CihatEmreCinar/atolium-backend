-- Check User record
SELECT "Id", "Email", "Role", "IsActive", "DeletedAt", "CreatedAt" 
FROM "Users" 
WHERE "Id" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';

-- Check EmployerProfile record
SELECT "Id", "UserId", "WorkshopTitle", "CreatedAt" 
FROM "EmployerProfiles" 
WHERE "UserId" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';
