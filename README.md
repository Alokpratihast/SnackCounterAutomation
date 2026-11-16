# Snack Counter Automation

A .NET 10 backend proof-of-concept for coordinating snack refills across Oslo and Bengaluru.

## Implemented workflow

- Office/product inventory with volunteer-entered stock counts and low-stock thresholds.
- One active refill request per office/product, round-robin assignment within the volunteer's home office, one-hour acceptance window, and expired-assignment reassignment.
- Purchase restricted to the volunteer assigned to an accepted request.
- Purchase, inventory increment, monthly budget commit, reservation ledger entry, and refill status transition are persisted in one SQLite write transaction.
- Duplicate purchase protection through lifecycle checks, an immediate SQLite write transaction, and a unique purchase-per-refill index.
- HR reimbursement stand-in with pending/submitted/rejected/reimbursed states, retry endpoint, and stable simulated claim IDs.
- Monthly spend reports preserve original currency totals and optionally convert using caller-supplied FX rates.
- Demo seed endpoint creates Oslo/Bengaluru offices, 15/20 volunteers, sample products, inventory and the configured monthly budgets.
- GitHub Actions restores, builds and tests the solution on feature pushes and pull requests.

## Business rules

| Office | Monthly budget | Currency |
|---|---:|---|
| Oslo | 6,000 | NOK |
| Bengaluru | 15,000 | INR |

- Office budgets are independent; NOK and INR are never added directly.
- A purchase is blocked when the monthly budget is missing, currency mismatches, or available budget is insufficient. There is no manager override.
- Only the assigned volunteer can accept and purchase; temporary visitors are not eligible in this MVP.
- Assignment acceptance window is one hour.
- Physical inventory counts remain volunteer-entered. A recorded purchase increments the expected stock; a later manual count remains the source of truth.
- Monthly budget and report boundaries currently use UTC because office time-zone identifiers are not part of the data model.

## Run locally

Prerequisite: .NET 10 SDK.

```bash
dotnet restore SnackCounterAutomation.slnx
dotnet build SnackCounterAutomation.slnx
dotnet test SnackCounterAutomation.slnx
dotnet run --project SnackCounter.API
```

The API applies EF Core migrations at startup. Default SQLite connection: `Data Source=snackcounter.db`.

## Quick demo

1. Start the API and call `POST /api/demo/seed`. Save the returned office IDs.
2. Read inventory from the SQLite database or use the seeded coffee product. Set a stock count with `PUT /api/inventory/{officeId}/{productId}/count`.
3. Create a low-stock refill using `POST /api/refill-requests/low-stock`. Save the request ID and assigned volunteer ID.
4. Accept with `POST /api/refill-requests/{requestId}/accept`.
5. Configure the current month budget with `PUT /api/budgets` if it has not been seeded.
6. Submit purchase with `POST /api/refill-requests/{requestId}/purchase`. Use a receipt URL/reference and the office currency.
7. If the simulated HR adapter fails, retry with `POST /api/purchases/{purchaseId}/retry-hr-claim`.
8. Simulate HR reconciliation with `POST /api/purchases/{purchaseId}/hr-reimbursed` or `POST /api/purchases/{purchaseId}/hr-rejected`.
9. Request the monthly report with `POST /api/reports/monthly/{officeId}`.

See [SnackCounter.API/SnackCounter.API.http](SnackCounter.API/SnackCounter.API.http) for request examples and [DESIGN.md](DESIGN.md) for decisions and limitations.

## Architecture

- `SnackCounter.Domain`: entities and business invariants.
- `SnackCounter.Application`: use cases and repository/integration contracts.
- `SnackCounter.Infrastructure`: EF Core/SQLite repositories, reporting, seed data and simulated HR/chat adapters.
- `SnackCounter.API`: Minimal API endpoints and dependency injection.
- `SnackCounter.Tests`: xUnit domain and persistence tests.

## MVP limitations / production hardening

- Simulated HR/chat only; real adapters require authentication, provider idempotency keys, retry/backoff and reconciliation.
- Receipt is a reference/URL; production should upload and validate image content in controlled object storage.
- API currently uses demo IDs in request bodies; add authentication and derive employee identity from claims before production.
- SQLite immediate transactions serialize purchase writes for this single-node MVP. For a multi-instance production deployment, use a database with row-level locking/serializable transactions and explicit concurrency handling.
- FX rates are supplied by the report caller. Persist source, effective timestamp and accounting policy before financial reporting.
- Add a scheduled worker for expired assignments and pending HR claims; current endpoints allow manual processing.
- Resolve outstanding NuGet vulnerability advisories before production.

## Assignment walkthrough (10 minutes)

1. 0:00–1:00 — Explain the two-office problem, separate currencies and constraints.
2. 1:00–2:30 — Seed Oslo/Bengaluru and show volunteer pools, budgets and inventory thresholds.
3. 2:30–4:00 — Trigger low stock, inspect fair assignment, demonstrate one-hour acceptance and expiry reassignment.
4. 4:00–6:00 — Accept request, record a purchase, show transactionally updated stock, budget and purchase ledger.
5. 6:00–7:00 — Demonstrate over-budget and duplicate-purchase rejection.
6. 7:00–8:00 — Show pending/submitted HR claim and simulated reimbursement/rejection reconciliation.
7. 8:00–9:00 — Show original currency monthly report and FX-converted report.
8. 9:00–10:00 — Run automated tests, explain limitations and production hardening.
