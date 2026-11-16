# Snack Counter Automation — Design

## Goal and constraints

Coordinate snack refills for two offices while preventing empty boxes, duplicate buying, uneven volunteer workload and reimbursement blind spots. The assignment describes approximately 15 volunteers in Oslo and 20 in Bengaluru. Employees may visit the other office, work hybrid or be on holiday.

HR accepts employee ID, amount, currency, description and receipt image. Approval may take 1–10 days and reimbursement is in payroll currency. Budgets are Oslo **6,000 NOK/month** and Bengaluru **15,000 INR/month**.

## End-to-end lifecycle

1. **Count stock:** volunteers manually update each office/product quantity. The system compares it with the minimum.
2. **Create refill:** a low-stock request is created. A filtered unique index prevents multiple active requests for the same office/product.
3. **Assign volunteer:** available volunteers from the product's home office are selected in round-robin order. A volunteer has one hour to accept. Expired assignments can be processed and reassigned.
4. **Purchase:** only the accepted, assigned volunteer may submit quantity, unit price, currency and receipt reference. The server calculates total; it does not trust a client total.
5. **Budget enforcement:** the current office/month budget must exist, currency must match the office, and available budget must cover the purchase. There is no override.
6. **Atomic persistence:** an immediate SQLite write transaction validates the request and volunteer, checks for an existing purchase, reserves and commits budget, inserts purchase and reservation ledger, increments inventory and transitions the refill to Purchased. Any database failure rolls the whole operation back.
7. **HR claim:** after the purchase transaction commits, a separate simulated HR call submits employee ID, amount, original currency, description and receipt reference. Failure leaves the claim Pending with a reason; the retry endpoint can resubmit. The simulated adapter uses a stable purchase-derived claim ID for idempotent retries.
8. **Reconcile:** HR can later mark a submitted claim reimbursed or rejected. Reimbursement updates the refill request to Reimbursed.
9. **Report:** monthly totals are grouped by original currency. A converted total is only produced when a positive FX rate is supplied for every source currency.

## Architecture

- **Domain:** Office, Employee, Product, Inventory, RefillRequest, Purchase, Budget, BudgetReservation and Reimbursement.
- **Application:** low-stock, assignment, acceptance, purchase and reimbursement submission use cases plus contracts.
- **Infrastructure:** EF Core/SQLite persistence, transaction repositories, simulated HR/chat adapters, demo seeding and monthly reporting.
- **API:** ASP.NET Core Minimal API; no frontend is required for this assignment.
- **Tests:** xUnit unit and SQLite persistence tests; GitHub Actions restores, builds and runs tests.

## Key decisions and trade-offs

- Inventory is office/product scoped. Purchase adds expected stock; a later volunteer-entered physical count remains authoritative.
- Volunteer pools are separate by home office. Temporary visitors are ineligible by default in this MVP.
- Budget overspend is blocked; no manager override.
- NOK and INR are never summed without explicit conversion.
- FX rates are caller-supplied for this MVP. A production system must store rate source, effective timestamp and accounting policy.
- Monthly period boundaries use UTC because the current Office model does not yet store an IANA time-zone ID.
- SQLite immediate write transactions serialize purchase operations for a single-node demo. Production multi-instance deployment should use a server database and row-level/serializable concurrency controls.
- Chat and HR are simulated. Production adapters need credentials, idempotency, retry/backoff, outbox processing and reconciliation.
- ReceiptUrl is a reference, not a secure upload implementation. Production must validate and store receipt images in controlled object storage.
- API accepts volunteer IDs for demonstration. Production must authenticate users and derive employee identity from claims.

## API surface

- `POST /api/demo/seed`
- `PUT /api/volunteers/{volunteerId}/availability`
- `PUT /api/inventory/{officeId}/{productId}/count`
- `POST /api/refill-requests/low-stock`
- `POST /api/refill-requests/{requestId}/accept`
- `POST /api/refill-requests/process-expired`
- `PUT /api/budgets`
- `POST /api/refill-requests/{requestId}/purchase`
- `POST /api/purchases/{purchaseId}/retry-hr-claim`
- `GET /api/purchases/{purchaseId}/reimbursement`
- `POST /api/purchases/{purchaseId}/hr-reimbursed`
- `POST /api/purchases/{purchaseId}/hr-rejected`
- `POST /api/reports/monthly/{officeId}`

## Operational notes

Run `dotnet restore`, `dotnet build`, `dotnet test`, then `dotnet run --project SnackCounter.API`. EF Core migrations apply at startup. The seed endpoint provisions two offices, 15/20 demo volunteers, three products per office, inventory and monthly budget defaults. See README and the API .http file for runnable requests.

## Remaining production work

Authentication/authorization, secure receipt upload, durable outbox and background workers, real HR/chat adapters, office time-zone support, persisted FX rates, database-specific concurrent load testing, audit history and dependency vulnerability remediation.
