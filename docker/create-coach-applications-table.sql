CREATE TABLE IF NOT EXISTS "CoachApplications" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Motivation" character varying(500) NOT NULL,
    "Experience" character varying(1000),
    "Specialization" character varying(500),
    "ImageUrls" character varying(2000),
    "Status" character varying(20) NOT NULL DEFAULT 'pending',
    "AdminNotes" character varying(500),
    "ReviewedBy" uuid,
    "ReviewedAt" timestamp with time zone,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CoachApplications" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_CoachApplications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_CoachApplications_Users_ReviewedBy" FOREIGN KEY ("ReviewedBy") REFERENCES "Users" ("Id") ON DELETE SET NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_CoachApplications_UserId" ON "CoachApplications" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_CoachApplications_ReviewedBy" ON "CoachApplications" ("ReviewedBy");

-- Add migration history entries
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20251205152014_AddCoachApplication', '8.0.0') 
ON CONFLICT DO NOTHING;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") 
VALUES ('20251205185536_AddImageUrlsToCoachApplication', '8.0.0') 
ON CONFLICT DO NOTHING;

