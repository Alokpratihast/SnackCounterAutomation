# Snack Counter Automation — Design

## Goal and constraints

Coordinate snack refills for two independent offices while preventing duplicate work, distributing volunteer work, enforcing local monthly budgets and creating a path to HR reimbursement. The assignment describes approximately 15 volunteers in Oslo and 20 in Bengaluru. Employees may visit the other office, work hybrid, or be on holiday. HR accepts amount, currency, description, receipt image and employee ID; approval may take 1–10 days and payroll reimbursement is in the employee's payroll currency.

Budgets: **Oslo — 6,000 NOK/month** (typical purchase 300–1,200 NOK); **Bengaluru — 15,000 INR/month** (typical purchase 500–3,000 INR).

## Proposed refill lifecycle

1. **Stock check:** A volunteer records the physical quantity for each product at their office. A scheduled/domain check compares it with the office-specific minimum.
2. **Create request:** When quantity is at/below minimum, create one open refill request for that office/product. Enforce one active request per office/product transactionally.
3. **Assign:** Select an available volunteer from the same home office using fair rotation. Notify them in internal chat with product, office, request and expiry.
4. **Accept:** The volunteer has **one hour** to accept. If not accepted, expire the assignment and offer the request to the next eligible volunteer. Preserve assignment history. If nobody is available, leave it pending and notify the office coordinator (policy to confirm).
5. **Purchase:** Only the accepted volunteer can submit a purchase. Validate product, quantity, unit price, total, currency and receipt. Reserve the office's remaining monthly budget before allowing purchase. If the budget would be exceeded, block the purchase; no manager override is included in this MVP.
6. **Record and replenish:** Record purchase and receipt, commit the reservation to actual spend, and update inventory. The physical count remains volunteer-entered; do not assume the delivery has physically reached the shelf.
7. **Claim:** Submit an HR claim with amount, original currency, description, receipt image and employee identifier. Store external claim ID and submission status. HR approval/reimbursement is asynchronous (1–10 days); reconcile it separately.
8. **Report:** Show monthly spend per office in original currency. A combined total requires an explicitly selected reporting currency and dated exchange rates. Never add NOK and INR as if they were the same unit.

## Components and data

| Component | Responsibility / key data |
|---|---|
| Inventory | Office, product, current quantity, minimum quantity, last count time |
| Refill request | Office, product, status, assigned volunteer, lifecycle timestamps |
| Volunteer assignment | Employee, home office, volunteer flag, availability, assignment history |
| Budget ledger | Office, year/month, currency, limit, actual spend, reserved amount |
| Purchase and receipt | Refill, buyer, quantity, unit price, total, currency, receipt reference, purchase time |
| Reimbursement | Purchase, external HR claim ID, status, submitted/reimbursed times, rejection reason |
| Chat adapter | Assignment/expiry notifications and delivery results for retry |
| HR adapter | Claim submission and later approval/payment reconciliation |
| Reporting | Office/month/currency totals plus conversion rate, rate date and reporting currency |

## Preventing the reported problems

- **Empty boxes:** minimum-stock thresholds create actionable refill requests.
- **Duplicate buying:** one active request per office/product; only the assigned and accepted volunteer may purchase; enforce with transactional conditional updates and unique constraints.
- **Unequal workload:** availability-aware round-robin assignment and assignment history.
- **Reimbursement visibility:** persist claim IDs and expose submitted/pending/approved/rejected/reimbursed states; HR controls actual turnaround.
- **Spend/consumption visibility:** retain office/product/month purchase and inventory records; report spend and consumption deltas.

## Trade-offs and deliberate non-builds

- **Manual stock counts:** avoids sensor complexity; accuracy depends on timely volunteer updates.
- **Office-local volunteer pools:** temporary visitors are not eligible by default, avoiding cross-office budget/responsibility ambiguity. Temporary-office authorization can be added later.
- **Hard budget block:** predictable; no override workflow in this version.
- **Simulated chat and HR:** runnable without credentials or external dependencies. Production adapters need authentication, retries, idempotency and audit logging.
- **No frontend:** focuses on backend/domain rules.
- **No FX engine:** exchange-rate provider, timestamp and accounting policy need agreement before consolidated totals.
- **No automated physical inventory:** volunteers remain the source of truth for counts.

## Implemented vs. remaining

**Implemented:** .NET 10 layered solution; domain entities for offices, employees, products, inventory, budgets/reservations, refill requests, purchases and reimbursements; volunteer selection constrained by home office and availability; one-hour timeout evaluation; budget reserve/commit/release rules; refill state transitions; SQLite mappings and selected unique indexes; console stand-ins for chat and HR; 11 passing unit tests.

**Not yet implemented/verified:** complete orchestration service and HTTP endpoints; low-stock scheduler; atomic transaction spanning assignment/purchase/budget/inventory; simultaneous-request concurrency test; full purchase/receipt workflow; durable chat/HR retries and status callbacks; cross-currency reporting; migrations, authentication and production deployment. The current API exposes only root and health endpoints. Unit tests and schema indexes are not proof that concurrent database requests cannot race.

## Production safeguards

Use EF Core migrations; add optimistic concurrency/conditional updates and transactions around assignment acceptance and purchase commit; make external calls idempotent with an outbox/inbox pattern; store receipts in controlled object storage; authorize by employee identity and office; audit state transitions; and resolve dependency vulnerability warnings before production.
