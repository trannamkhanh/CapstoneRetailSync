# Test report

Initial analysis: no application build or test had been executed. Teammate README test counts remain unverified claims. The PB-01 results below supersede the initial no-migration status.

Observed checks: local branch/status, tracked-file listing, cached remote inventories/history, source-file extraction and initial diagram inspection. Source documents/workbooks were not edited. Workbook recalculation and Excel rendering were not performed.

Required future evidence: POS validation/return/cancellation, duplicate and conflicting IDs, concurrent stock changes, rollback, FEFO allocation, lost ACK, HQ outages/recovery, durable POS WAN buffering, permanent rejection, server-side roles/store isolation, audit durability, TLS/mTLS, p95 under measured load and raw/aggregate byte reduction. Forecast, guardrail, decisions and nearby lookup tests follow approved scope.

## PB-01 execution on 31 October 2026

Environment: Docker Desktop 4.97.0, Engine 29.9.2, Compose 5.6.1, SQL Server 2025 Developer Linux image. Tested image digest: `mcr.microsoft.com/mssql/server@sha256:245f6b1c8903edae52cb20fa31419cefcde1a49938bb8c3447099011fc871ca3`.

Executed `docker compose config --quiet`, migration `up`, and `docker compose run --rm migrate test-runner`. Independently repeated the full suite from an empty database using Compose project `retailsync-pb01-check` on port 14334. Final clean-suite exit code: 0.

| Check | Observed result |
|---|---|
| Three migrations from empty RTS_DB | PASS; 001, 002, 003 applied |
| Immediate rerun | PASS; all three already applied |
| Applied migration changed | PASS; checksum mismatch refused |
| Applied version absent from checkout | PASS; history mismatch refused |
| Deliberate failure after CREATE TABLE | PASS; probe table and migration entry absent afterward |
| Concurrent migration reruns | PASS; both serialized and succeeded |
| Catalog against database design subset | PASS; 12 domain tables, 87 columns including types/nullability/identity, 12 PKs, 2 unique keys, 18 trusted FKs, no cascading FKs |
| Valid rows in all 12 tables | PASS |
| Invalid values/duplicates/orphan references | PASS; 32 expected database errors |
| Test fixture rollback | PASS; fixture store absent after rollback |
| Local secret excluded | PASS; git check-ignore .env |
| Compose configuration and tracked diff whitespace | PASS; no errors (Git emitted an informational LF/CRLF warning) |

Failures encountered and corrected: SQLCMD initially disabled QUOTED_IDENTIFIER, causing migration 002's filtered index creation to fail; added SQLCMD -I and reran. Catalog comparison initially encountered a database/catalog collation mismatch; explicit DATABASE_DEFAULT collation fixed verification. SQL fixture GUID conversion to varchar(20) required truncating a varchar(36) representation first. A separate test-server invocation omitted its port override and failed binding 14333; rerunning with 14334 succeeded. These were not ignored; the final clean suite passed afterward.

Four future-target FKs remain deliberately deferred. No permanent seed data was inserted, no teammate tests or WinForms builds ran, and no functional/NFR compliance is claimed. SQL test fixtures roll back but may advance identity counters. Normal development container remains healthy on port 14333; the independent test container was stopped without deleting its volume.
