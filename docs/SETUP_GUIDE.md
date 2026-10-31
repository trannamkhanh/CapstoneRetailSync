# Setup guide

PB-01 database setup is available in [database/README.md](../database/README.md). It includes exact Docker Compose initialization, migration, verification and SSMS/Visual Studio connection steps. No WinForms solution or backend service exists on Sang yet.

User reports Visual Studio 18.11.3. Observed host SDK: .NET 10.0.403. Docker Desktop 4.97.0, Engine 29.9.2, Compose 5.6.1 are available. PB-01 uses an isolated SQL Server 2025 Developer container with loopback port 14333 and a named data volume. No host SQL Server instance is modified. Candidate teammate POS targets .NET 8 Windows; compatibility is not yet verified locally.

Approved architecture will require WinForms client, FastAPI middleware, SQL Server RTS_DB and separate HQ mock. Keep database/token secrets in local environment configuration. Add exact build, migration, certificate and startup commands only after the real projects exist and commands are tested.
