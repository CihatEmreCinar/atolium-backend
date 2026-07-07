-- Check all records for this user
SELECT 'Users' as table_name, COUNT(*) as count FROM "Users" WHERE "Id" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';
SELECT 'EmployerProfiles' as table_name, COUNT(*) as count FROM "EmployerProfiles" WHERE "UserId" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';
SELECT 'EmployeeProfiles' as table_name, COUNT(*) as count FROM "EmployeeProfiles" WHERE "UserId" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';
SELECT 'CafeProfiles' as table_name, COUNT(*) as count FROM "CafeProfiles" WHERE "UserId" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';
SELECT 'Workshops' as table_name, COUNT(*) as count FROM "Workshops" WHERE "EmployerId" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';
SELECT 'Posts' as table_name, COUNT(*) as count FROM "Posts" WHERE "UserId" = 'abc8ea60-1357-4cfe-ab1a-8420994507a6';

-- Check if there are ANY EmployerProfiles in the database at all
SELECT COUNT(*) as total_employer_profiles FROM "EmployerProfiles";

-- Check the count of Users vs EmployerProfiles
SELECT 
  (SELECT COUNT(*) FROM "Users" WHERE "Role" = 'employer') as employer_users,
  (SELECT COUNT(*) FROM "EmployerProfiles") as employer_profiles;
