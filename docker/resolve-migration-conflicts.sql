-- Script to resolve migration conflicts
-- Drops MockInterviews table if it exists (from previous migration)

-- Drop MockInterviews table if it exists
DROP TABLE IF EXISTS "MockInterviews" CASCADE;

-- Remove migration history entries for conflicting migrations if needed
-- (This will be handled by EF Core migration system)

