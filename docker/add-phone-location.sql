-- Add PhoneNumber and Location columns to Users table
ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "PhoneNumber" VARCHAR(20),
ADD COLUMN IF NOT EXISTS "Location" VARCHAR(200);

