-- Seed admin user if it doesn't exist
INSERT INTO "Users" (
    "Id",
    "Email",
    "PasswordHash",
    "FirstName",
    "LastName",
    "Role",
    "EmailVerified",
    "CreatedAt",
    "UpdatedAt"
)
SELECT
    gen_random_uuid(),
    'admin@vector.com',
    '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', -- Admin@123
    'System',
    'Administrator',
    'admin',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Users" WHERE "Email" = 'admin@vector.com'
);

-- Verify admin user was created
SELECT "Email", "Role", "EmailVerified", "CreatedAt" FROM "Users" WHERE "Email" = 'admin@vector.com';

