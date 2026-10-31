# 1. Clone repository
cd C:\dev
git clone YOUR_GITHUB_REPOSITORY_URL
cd CapstoneRetailSync

# 2. Create local environment file (first setup only)
if (!(Test-Path .env)) {
    Copy-Item .env.example .env
}

# 3. Configure your database password
notepad .env

# 4. Validate Docker configuration
docker compose config --quiet

# 5. Start SQL Server
docker compose up -d --wait sqlserver

# 6. Apply database migrations
docker compose run --rm migrate up

# 7. Verify database schema
docker compose run --rm migrate verify

# 8. Run database constraint tests
docker compose run --rm migrate test

# 9. Run complete migration test suite
docker compose run --rm migrate test-runner

# 10. Check migration status
docker compose run --rm migrate status

# 11. Check running containers
docker compose ps
