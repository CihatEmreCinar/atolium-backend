-- Check Workshop details for this user
SELECT "Id", "EmployerId", "EmployerProfileId", "Title", "Status", "CreatedAt" 
FROM "Workshops" 
WHERE "EmployerId" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6'
LIMIT 5;

-- Check which Users don't have EmployerProfiles
SELECT u."Id", u."Email", u."Role", u."CreatedAt"
FROM "Users" u
WHERE u."Role" = 'employer' 
AND NOT EXISTS (SELECT 1 FROM "EmployerProfiles" ep WHERE ep."UserId" = u."Id")
ORDER BY u."CreatedAt";

-- Check which EmployerProfiles DO exist
SELECT "Id", "UserId", "WorkshopTitle", "CreatedAt" 
FROM "EmployerProfiles" 
ORDER BY "CreatedAt";
