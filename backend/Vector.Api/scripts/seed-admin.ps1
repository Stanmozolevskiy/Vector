# Script to manually seed admin user in the database
# Usage: .\seed-admin.ps1

Write-Host "Seeding admin user..." -ForegroundColor Cyan

# Get connection string from appsettings.json or environment
$connectionString = $env:ConnectionStrings__DefaultConnection
if (-not $connectionString) {
    $connectionString = "Host=localhost;Port=5432;Database=vector_db;Username=postgres;Password=postgres"
}

Write-Host "Connection: $connectionString" -ForegroundColor Gray

# Connect to PostgreSQL and create admin user
$query = @"
DO `$
DECLARE
    admin_exists BOOLEAN;
    admin_id UUID;
    password_hash TEXT;
BEGIN
    -- Check if admin user exists
    SELECT EXISTS(SELECT 1 FROM "Users" WHERE email = 'admin@vector.com') INTO admin_exists;
    
    IF admin_exists THEN
        RAISE NOTICE 'Admin user already exists. Skipping.';
    ELSE
        -- Generate password hash (BCrypt hash for 'Admin@123')
        -- Note: This is a pre-computed BCrypt hash. In production, use PasswordHasher.HashPassword()
        password_hash := '\$2a\$11\$KIXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXxXx';
        
        -- Generate new UUID
        admin_id := gen_random_uuid();
        
        -- Insert admin user
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
        ) VALUES (
            admin_id,
            'admin@vector.com',
            '\$2a\$11\$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', -- Admin@123
            'System',
            'Administrator',
            'admin',
            true,
            NOW(),
            NOW()
        );
        
        RAISE NOTICE 'Admin user created successfully!';
        RAISE NOTICE 'Email: admin@vector.com';
        RAISE NOTICE 'Password: Admin@123';
    END IF;
END
`$;
"@

# For PowerShell, we'll use a simpler approach via psql
$psqlCommand = @"
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
    '\$2a\$11\$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy', -- Admin@123
    'System',
    'Administrator',
    'admin',
    true,
    NOW(),
    NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM "Users" WHERE email = 'admin@vector.com'
);
"@

Write-Host "`nTo seed admin user manually, run this SQL in pgAdmin or psql:" -ForegroundColor Yellow
Write-Host $psqlCommand -ForegroundColor White
Write-Host "`nOr use Docker:" -ForegroundColor Yellow
Write-Host "docker exec -it vector-postgres psql -U postgres -d vector_db -c `"$psqlCommand`"" -ForegroundColor White

