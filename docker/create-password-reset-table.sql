CREATE TABLE IF NOT EXISTS "PasswordResets" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Token" text NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "IsUsed" boolean NOT NULL DEFAULT false,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
    CONSTRAINT "PK_PasswordResets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PasswordResets_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PasswordResets_Token" ON "PasswordResets" ("Token");

