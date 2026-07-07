-- Create missing EmployerProfile records for employer users who don't have one
INSERT INTO "EmployerProfiles" ("Id", "UserId", "WorkshopTitle", "Specialization", "YearsExperience", "AvgRating", "TotalWorkshops", "EmployerRank", "CreatedAt")
SELECT 
  gen_random_uuid() as "Id",
  u."Id" as "UserId",
  '' as "WorkshopTitle",
  ARRAY[]::text[] as "Specialization",
  NULL as "YearsExperience",
  0 as "AvgRating",
  0 as "TotalWorkshops",
  'beginner' as "EmployerRank",
  NOW() as "CreatedAt"
FROM "Users" u
WHERE u."Role" = 'employer' 
AND NOT EXISTS (SELECT 1 FROM "EmployerProfiles" ep WHERE ep."UserId" = u."Id");

-- Verify the fix
SELECT 
  (SELECT COUNT(*) FROM "Users" WHERE "Role" = 'employer') as employer_users,
  (SELECT COUNT(*) FROM "EmployerProfiles") as employer_profiles;
  
-- Show the created profiles
SELECT "Id", "UserId", "CreatedAt" FROM "EmployerProfiles" ORDER BY "CreatedAt" DESC LIMIT 5;
