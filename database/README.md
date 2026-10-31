# SQL Server migration foundation

Implements Sprint 2 PB-01-1 through PB-01-5. This is the regional RTS_DB schema foundation, not a POS implementation or an HQ database. Quang's POS code is unchanged.

## Start from PowerShell

Run at `C:\dev\CapstoneRetailSync` with Docker Desktop running Linux containers:

```powershell
# First setup only. Do not overwrite an existing .env.
Copy-Item .env.example .env
notepad .env
# Replace the password placeholder with a unique strong development password.
docker compose config --quiet
docker compose up -d --wait sqlserver
docker compose run --rm migrate up
docker compose run --rm migrate verify
docker compose run --rm migrate test
docker compose run --rm migrate status
```

For the current workspace, `.env` has already been generated with a random password and is ignored by Git. Do not recopy the example over it. Do not share or commit it. `docker compose config` without `--quiet` prints resolved secrets; avoid including that output in reports.

The setup uses Microsoft's SQL Server 2025 Developer Linux image, including its SQLCMD/ODBC 18 tools. No host SQL Server, Python dependency installation or WinForms build is required for PB-01. The host .NET SDK observed was 10.0.403; framework selection is a later frontend decision.

SSMS or Visual Studio **View > SQL Server Object Explorer > Add SQL Server**: server `127.0.0.1,14333`, SQL Server Authentication, user `sa`, password from your local `.env`, database `RTS_DB`. Enable encryption and trust the server certificate for this local development container. This development setup uses `sqlcmd -C` for its self-signed certificate; production must validate a trusted server certificate and use least-privilege application/migration identities. No application credentials are created by PB-01.

`sqlserver` exposes only loopback, uses a named data volume and waits for a successful authenticated healthcheck before migration starts. If port 14333 is occupied, set SQLSERVER_PORT in `.env`. If startup fails, inspect `docker compose ps` and `docker compose logs --tail 80 sqlserver`; do not delete existing database volumes to fix startup. The password used at initial volume creation remains the database password even if the environment variable is later changed.

## Migration tool

The repository-local `database/migrate.sh` runs through the `migrate` Compose service using Microsoft's SQLCMD, avoiding an additional backend dependency just to apply SQL. It is forward-only. Migrations are UTF-8 with LF endings, use `NNN_description.sql`, and contain T-SQL without GO or sqlcmd directives. Each file executes in its own transaction. A session application lock serializes runners; the same lock protects checksum/history verification. Database creation is separately locked in master.

`dbo.__schema_migration` is infrastructure metadata, in addition to the 12 domain tables. It stores filename, SHA-256 and UTC application time. Reruns skip applied files; modified or missing applied files fail. Failed DDL and its history record roll back together. Do not edit an applied migration: add the next numbered file. No automatic drop, downgrade or destructive reset command is provided.

SQLCMD uses `-b` to return a failure code and `-I` for QUOTED_IDENTIFIER, required by filtered indexes. Credentials are passed through the container environment, not command-line password arguments. Initialization connects to a local development SQL Server only; this Compose stack is not the complete private-WAN/mTLS deployment.

Microsoft runtime reference: https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker?view=sql-server-ver17

## Schema mapping and scope

The user approved the database document's physical names: backlog `transaction` maps to `pos_transaction`; backlog `alert_threshold` maps to `near_expiry_config` (not created in PB-01). No parallel aliases or renamed teammate tables are introduced.

| Task | Migration/deliverable | Database design v1.1 tables |
|---|---|---|
| PB-01-1 | compose.yaml, migrate.sh, version/checksum history | Tooling |
| PB-01-2 | 001_registry_inventory.sql: store, pos_terminal, staff_user, product, inventory | 2,3,4,6,8 |
| PB-01-3 | 002_transactions_sync.sql: sync_batch, pos_transaction, transaction_item, inventory_movement | 9,10,11,12 |
| PB-01-4 | 003_recommendations_audit.sql: recommendation, guardrail_rule, audit_log | 5,17,18 |
| PB-01-5 | verification/schema.sql and constraints.sql | Exact current-stage column/key/relationship checks |

Types, lengths, decimal precision and identity keys follow the detailed database design; inventory uses composite PK (store_id,sku). Duplicate transaction/batch IDs, usernames and barcodes have database uniqueness protection. Foreign keys use NO ACTION to avoid deleting historical records. Indexes support inventory lookup by SKU, unbatched transactions, original-sale references, sync queues, movement history, recommendations and audit queries.

The source does not give a complete NULL/default matrix. Implementation choices: required identifiers/business fields are NOT NULL; documented optional links, error/retry/ACK fields, byte counts not yet measured, audit details, manual movement fields and recommendation discount are nullable. Times and statuses have no invented defaults; callers must supply them (UTC for datetime2). Only the migration journal sets its own UTC timestamp. No password column is introduced for pre-issued demo JWT accounts.

Checks enforce documented domains, coordinate ranges, nonnegative prices/inventory, positive item quantities, valid periods/nonnegative counters, discounts in 0-100 and nonnegative policy limits. Signed movement quantities and potentially negative net sales effects are not clamped. Role values follow the database's seven physical names; endpoint permission mapping is not implemented here.

## Deferred relationships

PB-01 creates 12 of the 19 regional tables. The remaining seven domain tables and HQ tables are later work. All current-target FKs exist and are checked. Four columns are retained with the exact designed types, but their FKs cannot exist yet:

| Current column | Later target | Work item |
|---|---|---|
| inventory_movement.batch_id | product_batch.batch_id | PB-20 |
| recommendation.batch_id | product_batch.batch_id | PB-20 |
| recommendation.alert_id | near_expiry_alert.alert_id | PB-21 |
| recommendation.forecast_id | forecast.forecast_id | PB-22 |

Keep these columns NULL until their feature tables are introduced. Future migrations must add FKs WITH CHECK, first resolving any non-null orphan data. This is explicit staged ERD coverage, not a claim that the complete 19-table ERD is implemented. `sales_summary_line`, `product_batch`, `forecast`, `near_expiry_alert`, `near_expiry_config`, `guardrail_result` and `staff_decision` remain pending. All FR01-FR09 are required by the user despite some stories being Unplanned in the workbook.

Application logic must later enforce terminal/store matching, same-store original-sale validation, return limits, cancellation rules, atomic stock/movement/event/audit persistence, canonical payload hashes and guardrail authorization. Schema creation alone does not implement those requirements. Cross-store consistency of future batch links must also be checked by the feature service.

## Verification

```powershell
docker compose run --rm migrate test-runner
```

Run this suite against development/test data only. It checks safe reruns, rejects altered/missing migrations, injects a deliberately failing migration to prove DDL/history rollback, runs concurrent reruns, compares catalog metadata, and exercises constraints. Fixture rows in all 12 tables are rolled back. SQL Server IDENTITY counters can advance during rollback; tests do not assume gap-free IDs. No permanent demo seed is inserted, avoiding assumptions about Quang's sample IDs and data.

The schema check compares 87 column definitions, nullability/identity, 12 PKs, 2 unique keys, 18 FKs and constraint trust. It also rejects unexpected tables and cascading FKs for this PB-01 baseline. Extend the verification baseline alongside future feature migrations. The constraint suite tests valid inserts and 32 invalid operations, including duplicate keys, orphan references, negative stock, bad enum values and price/discount boundaries. It is not a test of business services or NFR performance targets.

To independently test a clean install, use a separate Compose project and unused port (without resetting the main database):

```powershell
$env:SQLSERVER_PORT = '14334'
docker compose -p retailsync-pb01-check up -d --wait sqlserver
docker compose -p retailsync-pb01-check run --rm migrate test-runner
docker compose -p retailsync-pb01-check stop sqlserver
Remove-Item Env:\SQLSERVER_PORT
```

Stop the normal development server with `docker compose stop sqlserver`; restart with `docker compose up -d --wait sqlserver`. Data remains in its named volume. Back up meaningful data before future migrations. SQL Server image tag `2025-latest` is a major-version development baseline; capture/approve an image digest for a release environment.
