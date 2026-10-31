# Architecture decisions

User direction: retain FastAPI regional middleware and SQL Server; use WinForms as desktop presentation. Full .NET backend migration requires explicit approval. Exact framework is undecided.

Confirmed by user: all FR01-FR09 are required in Capstone 1; retain FastAPI, WinForms and SQL Server; Quang's POS branch is the integration candidate. Do not treat Unplanned workbook stories as approved scope deferrals. Four-sprint capacity needs replanning.

PB-01 decisions: user confirmed no teammate regional schema and approved physical names from the database design (`pos_transaction`, `near_expiry_config`). Use forward-only versioned SQL with a repository-local SQLCMD runner in Docker Compose; this adds no Python backend dependency. Create the 12 requested domain tables and defer four foreign keys to tables belonging to later stories, documented in database/README.md. Do not introduce placeholder feature tables.

Remaining approvals: seven-role endpoint permission matrix and conflicting retry-policy wording. Exact frontend target awaits compatibility review. Installed .NET SDK observed: 10.0.403. SQL Server development container uses the 2025 major-version image.
