# Requirements traceability

Initial requirement map. PB-01 schema foundation is implemented; functional FR/NFR evidence remains pending. UC IDs below are SRS IDs, not architecture numbering. User requires all nine FR regardless of Unplanned workbook entries.

| Requirement | SRS use case | Backlog IDs | Planned v1.3 sprint |
|---|---|---|---|
| FR01 POS/inventory | UC.01 | PB-05/06/07/08/10/12; PB-11 optional | 2; PB-11 Unplanned |
| FR02 aggregation/sync | UC.02 | PB-14/15/18 | 3 |
| FR03 idempotency | UC.03 | PB-09/16 | 2/3 |
| FR04 expiry | UC.04 | PB-20/21/34/43 | 3/4; 34/43 Unplanned |
| FR05 forecast | UC.05 | PB-22/23/24 | Unplanned |
| FR06 recommendations/guardrails | UC.06 | PB-25/26/27/28 | Unplanned |
| FR07 staff decisions | UC.07 | PB-32/33/47 | Unplanned |
| FR08 nearby stock | UC.08 | PB-35/46 | Unplanned |
| FR09 offline HQ | UC.09 | PB-17; PB-19 optional | 3; 19 Unplanned |
| NFR01 latency | UC.01 | PB-05/39 | 2/4 |
| NFR02 zero loss/double count | UC.01/02/03/09 | PB-09/16/17/37 | 2/3/4 |
| NFR03 HQ outage continuity | UC.09 | PB-17/37 | 3/4 |
| NFR04 security/audit | Cross-cutting | PB-02/04/48 | 2 |
| NFR05 no stale overwrite | UC.02/09 | PB-13/37 | 3/4 |
| Data reduction >=90% | UC.02 | PB-40 | 4 |

Support: PB-01 migrations; PB-03/45 POS simulation; PB-37/38/39/40 validation; PB-41/42 fixes and delivery. Add actual component paths, test names and executed evidence as implementation arrives.

| Completed task | Implementation | Observed verification |
|---|---|---|
| PB-01-1 | compose.yaml; database/migrate.sh | Clean install, reruns, checksum/missing-version rejection, rollback and concurrency |
| PB-01-2 | database/migrations/001_registry_inventory.sql | Catalog metadata, PK/UQ/FK, valid fixtures and invalid values |
| PB-01-3 | database/migrations/002_transactions_sync.sql | Transaction/batch uniqueness, references, quantities and statuses |
| PB-01-4 | database/migrations/003_recommendations_audit.sql | Metadata, recommendations/rules/audit inserts, policy limits |
| PB-01-5 | database/verification/schema.sql; constraints.sql; runner.sh | 12 tables, 87 columns, 18 FKs; 32 rejection scenarios |

PB-01 supports FR01/FR02/FR03/FR06 and NFR02/NFR04 through schema constraints and persistence structures, not implemented endpoint behavior. Exact type/relationship coverage and four deferred FKs are documented in database/README.md. Later stories must add the remaining seven tables and complete their links.
