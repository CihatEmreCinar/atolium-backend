-- Verify the fix: Show the EmployerProfile for abc8ea60-...
SELECT 
  ep."Id" as profile_id,
  ep."UserId" as user_id,
  u."Email" as user_email,
  u."Role" as user_role,
  ep."CreatedAt" as profile_created
FROM "EmployerProfiles" ep
JOIN "Users" u ON u."Id" = ep."UserId"
WHERE u."Id" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';

-- Verify that all employer users now have profiles
SELECT 
  (SELECT COUNT(*) FROM "Users" WHERE "Role" = 'employer') as employer_users_total,
  (SELECT COUNT(*) FROM "EmployerProfiles") as employer_profiles_total,
  (SELECT COUNT(*) FROM "Users" u WHERE u."Role" = 'employer' AND EXISTS(SELECT 1 FROM "EmployerProfiles" ep WHERE ep."UserId" = u."Id")) as employer_users_with_profile;
