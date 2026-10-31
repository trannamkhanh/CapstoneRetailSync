# RetailSync initial analysis

Analysis date: 31 October 2026. This is an initial specification and integration review, not an implementation or acceptance sign-off.

## Sources and review coverage

All six supplied files were accessible in `C:/Users/Andrei Harkov/Downloads/Capstone documents`:

- Software_Requirement_Specification_C1SE_24_v2.docx: FR01-FR09, NFR01-NFR05, UC.01-UC.09, scope and business flows.
- ArchitectureDesign_ver1.1.docx: regional service boundaries, deployment and technical constraints.
- Database_Design_and_Data_Mapping_Document__RetailSync__v1.1.docx: 19 regional tables, two separate HQ tables, field definitions and mapping/validation tables.
- C1SE.24_Project Plan_ver_1.1.pdf: project organization, resources, risks and deliverables; all 14 pages text-extracted.
- RetailSync_Product_Backlog_v1_1.xlsx: older six-sprint plan and SRS alignment log.
- RetailSync_Sprint_Backlog.xlsx: internally version 1.3 dated 31 October 2026, four-sprint plan, task owners and acceptance criteria.

DOCX paragraphs/tables and workbook sheets/formulas/cached values were extracted read-only. Embedded DOCX diagrams were reviewed as image sheets. Some SRS diagrams have black backgrounds with poorly visible labels; fine detail is not fully verified from these previews. Original-resolution inspection or accessible diagram exports are needed before implementing ambiguous diagram-only details. Workbook calculations were not recalculated or validated in Excel. No source file was modified.

## Repository evidence

Sang at 8e3983d tracks origin/Sang. Only an empty .gitignore is tracked; .vs/ is untracked. These findings use cached remote refs, not a fresh fetch.

Cached origin/Khanh at 83ba526 contains a .NET 8 WinForms Demo POS implementation and tests (PB-45). Cached origin/Quang at ddd7777 contains another Demo POS version, including SQLite persistence and tracked build output. README/API-client inspection establishes candidate integration work, not verified compatibility. No branch was checked out or merged. No backend or SQL Server implementation was found in the inspected branch inventories.

## Scope reconciliation

The SRS describes FR01-FR09, with forecasting, recommendations, guardrails, staff decisions and cross-store lookup included in the broader product scope. Its frontend is a thin three-screen Staff App; no login or account-management API is included. Demo JWTs are pre-issued. Dashboards, full sync history and several optional extensions are not required MVP screens.

The latest sprint workbook schedules all Must stories plus PB-20/PB-21. PB-22-PB-28, PB-31-PB-35 and PB-46/PB-47 are Unplanned, among others. Thus the latest scheduled delivery does not cover every SRS FR or the proposed WinForms Staff App. Unplanned does not mean implemented or formally removed from the SRS. Obtain scope approval before selecting either the reduced delivery or full SRS scope.

Recommended scheduling baseline, pending confirmation: sprint workbook v1.3. Retain the SRS as the behavior baseline and record approved deferrals explicitly. The product workbook v1.1 remains useful for story criteria, but its old dates/Done statuses cannot prove current implementation.

| Sprint | Dates in v1.3 | Planned work and traceability |
|---|---|---|
| 1 | 9-30 Oct | Design DS-01 through DS-10; workbook says Done, underlying deliverables need collection |
| 2 | 31 Oct-13 Nov | PB-01,02,04,05,06,07,08,09,10,12,48: persistence, security, registry, POS, idempotency, inventory and audit; FR01/03, NFR01/02/04 |
| 3 | 14-27 Nov | PB-03,13,14,15,16,17,18,20,45: simulation, delta aggregation, HQ, retries/offline and FEFO; FR01/02/03/04/09, NFR02/03/05 |
| 4 | 28 Nov-11 Dec | PB-21,37,38,39,40,41,42: alerts/configuration, integration/UAT/performance, metrics and delivery; FR04 and NFR01-NFR05 |

Within Sprint 2: collect existing DDL/OpenAPI first, agree contract differences, establish environment/migrations/security, then implement registry and atomic SALE with idempotency/audit, followed by RETURN/CANCELLATION and queries. Test concurrency, rollback and duplicates as part of those changes. Establish security from the foundation and FEFO-compatible allocations before the later batch implementation.

If full SRS coverage is approved, add explicit capacity and owners for FR05 (PB-22/23/24), FR06 (PB-25/26/27/28), FR07 (PB-32/33/47), FR08 (PB-35/46), and staff/alert APIs PB-31/34. Do not imply these fit the existing dates without replanning.

## Architecture and proposed solution layout

Keep FastAPI, SQL Server, Python forecasting, REST/JSON and Docker. WinForms replaces the thin Staff App presentation only, subject to recording the browser-to-desktop deviation. The regional database remains authoritative. No form directly changes regional stock or runs HQ sync rules.

Proposed paths, not created:

```text
RetailSync/
  frontend/
    RetailSync.Desktop.sln
    staff-winforms/RetailSync.Staff.WinForms/
    shared/RetailSync.ApiClient/
    shared/RetailSync.Contracts/
    tests/RetailSync.Desktop.Tests/
    demo-pos-winforms/             existing teammate candidate
  backend/app/
    api/ schemas/ services/ domain/ repositories/ integrations/ jobs/ core/
  backend/tests/
  hq-mock/
  database/migrations/
  database/seeds/
  contracts/
  tools/pos-simulator/
  deploy/
docs/
```

Visual Studio manages the C# solution; backend/HQ are separate Python processes or Docker services. Use REST DTOs/OpenAPI as the language boundary. No C# backend project is proposed. Exact target framework awaits installed Visual Studio/SDK information and teammate compatibility review; teammate POS currently targets net8.0-windows.

Required interfaces in database specification section 5.4: POST /api/pos/events, GET /api/stores/{store_id}/inventory, GET /api/products/{sku}/stock?from_store={store_id}, POST /api/sync/run (administrator), and POST /api/sync at HQ. Staff recommendation/decision, barcode, alert, admin configuration and auditor routes require the missing DS-04 OpenAPI deliverable. Do not invent their final paths.

POS traffic uses private WAN, TLS 1.2+, mTLS and JWT. Staff uses HTTPS/JWT and server-side store/role authorization. Only the sync integration calls HQ. Scheduled jobs run in-process; prevent duplicate execution if deployment starts multiple worker/API processes.

## Database and data flows

The 19 regional tables are store, pos_terminal, staff_user, audit_log, product, product_batch, inventory, inventory_movement, pos_transaction, transaction_item, sync_batch, sales_summary_line, forecast, near_expiry_alert, near_expiry_config, recommendation, guardrail_rule, guardrail_result and staff_decision. HQ owns hq_sync_batch and hq_sales_total outside RTS_DB.

Preserve the detailed database document's field types/keys, including inventory PK (store_id, sku), txn_id and sync_batch_id idempotency keys, canonical payload_hash, unique summary (sync_batch_id, sku), and unique staff_decision.rec_id. No migrations have been generated.

1. POS event: authenticate/validate, detect duplicate or conflicting payload, then persist transaction/items, movements, inventory changes and successful-action audit atomically. A rejection/duplicate attempt still needs a durable audit entry without committing rejected stock changes.
2. SALE reduces stock, eventually using FEFO allocations. RETURN adds only quantities still returnable. CANCELLATION reverses a sale and is prohibited after returns or prior cancellation under database rules.
3. Aggregation atomically assigns unbatched events to one store batch and SKU summaries. Include sale/return/cancellation effects; do not drop cancelled original sales and thereby count only their reversal. Empty stores get no batch.
4. Delivery: PENDING -> SENT -> ACKED; timeout/5xx returns to PENDING with backoff. Invalid 4xx/NACK -> FAILED with no automatic resend; it does not block valid later batches. Resend the same ID after lost ACK; HQ updates totals once.
5. Expiry uses category thresholds and one OPEN alert per batch. Forecast compares stock minus predicted demand; results are SURPLUS/SHORTAGE/BALANCED. Recommendations begin GENERATED, become BLOCKED or PENDING after all guardrails, and may receive one immutable staff decision.
6. Cross-store lookup is read-only within the region and ordered by haversine distance.

## Conflicts and missing decisions

- Schedule/scope: SRS and architecture describe six sprints; latest workbook has four, 4 h/member/day and coding restart 31 October. SRS says release 4 December; project/architecture reference final submission 13 December; latest sprint ends 11 December. Confirm formal deadlines and scope authority.
- Names: backlog uses transaction and alert_threshold, database uses pos_transaction and near_expiry_config. Recommend explicit conceptual-to-physical mapping, preserving database names after approval.
- Use cases: architecture UC01-UC15 differ from SRS UC.01-UC.09. Use SRS IDs for traceability and retain a crosswalk rather than treating matching numbers as equivalent.
- Security: SRS/backlog refer to six roles; database distinguishes seven physical roles, splitting SYSTEM_ADMIN and BUSINESS_ADMIN. Architecture assigns some guardrail configuration to Store Manager whereas SRS/database assign Business Admin. Approve an endpoint permission matrix.
- Retry: older architecture retries NACK and mentions maximum attempts; database/latest criteria classify invalid 4xx as permanent and keep transient failures queued. Recommend detailed database/SRS classification; approve conflict resolution before implementation.
- Frontend: browser description versus requested WinForms, plus UI stories Unplanned. No login backend should be introduced merely to provide a login screen.
- Teammate API: README uses /api/v1 plus /pos/events and event_type/quantity/unit_price/original_transaction_id; database mapping uses /api/pos/events and type/qty/price/ref_txn_id. Freeze OpenAPI before adapting either side.
- Teammate cancellation documentation permits residual reversal after partial return; database V09 forbids cancellation after any return. Preserve code until reviewed with the owner; align behavior to the approved contract.
- SQLite POS mode is a simulator candidate, not a substitute for regional SQL Server. Demo correctness does not prove middleware consistency.
- Missing DS-03 DDL, DS-04 OpenAPI, DS-06 model/features and DS-07 rule defaults despite Done design tasks. Forecast model family is named, but exact baseline, minimum history, horizon, features and evaluation split need the actual design deliverable.
- Clarify batch allocation of returns/cancellations, expired-batch sale policy, alert refresh/lifecycle, canonical hash ordering, startup recovery of SENT batches and multi-worker scheduling before affected work.
- Migration sequencing: planned Sprint 2 recommendation tables reference later alert/forecast tables. Decide how to stage foreign keys without weakening the final schema.
- Metrics: p95 POS <500 ms at up to 50 ms WAN RTT, zero loss/double counting, local operation through HQ loss, TLS/RBAC/audit and no snapshot overwrite require measured tests. Define load, dataset and raw-vs-aggregate byte measurement for the >=90% reduction target. No compliance result is available yet.

## Next decision checkpoint

Confirm teammate ownership/code and authoritative four-sprint scope first. Obtain missing design deliverables and local tool versions. Then create the approved nonconflicting foundation on Sang. No source generation, integration, commit or push has occurred in this session.
