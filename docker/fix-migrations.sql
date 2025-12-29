-- Fix migration conflicts
-- Drop MockInterviews table
DROP TABLE IF EXISTS "MockInterviews" CASCADE;

-- Mark conflicting migrations as applied
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20251228150916_RemovePeerInterviewEntities', '8.0.0') 
ON CONFLICT ("MigrationId") DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20251228151517_RemoveMockInterviewEntity', '8.0.0') 
ON CONFLICT ("MigrationId") DO NOTHING;

