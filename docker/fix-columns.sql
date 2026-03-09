-- Fix SiteSettings column names to PascalCase
ALTER TABLE "SiteSettings" RENAME COLUMN id TO "Id";
ALTER TABLE "SiteSettings" RENAME COLUMN key TO "Key";
ALTER TABLE "SiteSettings" RENAME COLUMN value TO "Value";
ALTER TABLE "SiteSettings" RENAME COLUMN updatedat TO "UpdatedAt";
ALTER TABLE "SiteSettings" RENAME COLUMN updatedbyuserid TO "UpdatedByUserId";
