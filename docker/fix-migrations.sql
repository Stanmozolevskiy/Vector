-- AddCommentTypeField (20260212191327)
ALTER TABLE "InterviewQuestionComments" ADD COLUMN IF NOT EXISTS "CommentType" character varying(50);
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260212191327_AddCommentTypeField', '8.0.0') ON CONFLICT DO NOTHING;

-- AddVoteTypeToCommentVotes (20260215215958)
ALTER TABLE "InterviewQuestionCommentVotes" ADD COLUMN IF NOT EXISTS "VoteType" integer NOT NULL DEFAULT 0;
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260215215958_AddVoteTypeToCommentVotes', '8.0.0') ON CONFLICT DO NOTHING;

-- AddDailyChallenges (20260222155734)
CREATE TABLE IF NOT EXISTS "DailyChallenges" (
    "Id" uuid NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    "QuestionId" uuid NOT NULL,
    "Difficulty" character varying(50) NOT NULL,
    "Category" character varying(100) NOT NULL,
    "AttemptCount" integer NOT NULL DEFAULT 0,
    "CompletionCount" integer NOT NULL DEFAULT 0,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsActive" boolean NOT NULL DEFAULT false,
    CONSTRAINT "PK_DailyChallenges" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_DailyChallenges_InterviewQuestions_QuestionId" FOREIGN KEY ("QuestionId") REFERENCES "InterviewQuestions"("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_DailyChallenges_Date" ON "DailyChallenges" ("Date");
CREATE INDEX IF NOT EXISTS "IX_DailyChallenges_Date_IsActive" ON "DailyChallenges" ("Date", "IsActive");
CREATE INDEX IF NOT EXISTS "IX_DailyChallenges_QuestionId" ON "DailyChallenges" ("QuestionId");

CREATE TABLE IF NOT EXISTS "UserChallengeAttempts" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ChallengeId" uuid NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    "IsCompleted" boolean NOT NULL DEFAULT false,
    "TimeSpentSeconds" integer,
    "Language" character varying(50),
    "Code" text,
    "TestCasesPassed" integer NOT NULL DEFAULT 0,
    "TotalTestCases" integer NOT NULL DEFAULT 0,
    "CoinsEarned" integer NOT NULL DEFAULT 0,
    CONSTRAINT "PK_UserChallengeAttempts" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserChallengeAttempts_DailyChallenges_ChallengeId" FOREIGN KEY ("ChallengeId") REFERENCES "DailyChallenges"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_UserChallengeAttempts_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_UserChallengeAttempts_ChallengeId" ON "UserChallengeAttempts" ("ChallengeId");
CREATE INDEX IF NOT EXISTS "IX_UserChallengeAttempts_CompletedAt" ON "UserChallengeAttempts" ("CompletedAt");
CREATE INDEX IF NOT EXISTS "IX_UserChallengeAttempts_UserId" ON "UserChallengeAttempts" ("UserId");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_UserChallengeAttempts_UserId_ChallengeId" ON "UserChallengeAttempts" ("UserId", "ChallengeId");
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260222155734_AddDailyChallenges', '8.0.0') ON CONFLICT DO NOTHING;

-- AddSiteSettings (20260308144136) - table already created
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260308144136_AddSiteSettings', '8.0.0') ON CONFLICT DO NOTHING;
