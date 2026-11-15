# Snack Counter Automation

A .NET 10 proof-of-concept for coordinating snack refills across the Oslo and Bengaluru offices.

## Current implementation

- Domain entities for offices, employees, products, inventory, budgets/reservations, refill requests, purchases and reimbursements.
- Volunteer selection restricted to available volunteers from the refill's home office, with simple alphabetical rotation.
- One-hour assignment timeout evaluation service.
- Budget reserve, commit, release and overspend rules.
- EF Core/SQLite persistence mappings and selected unique indexes.
- Console-based simulated chat and HR claim adapters.
- xUnit unit tests (11 passed in the last verified run).

### Scope and limitations

This is not yet a complete end-to-end application. The API currently exposes only `/` and `/health`. Refill orchestration endpoints, low-stock detection, purchase submission, real chat/HR integrations and consolidated cross-currency reporting are not wired into an executable workflow. Current tests are unit tests; they do not prove database-level concurrent request safety or cross-currency report totals.

## Technology

- .NET 10 / C#
- ASP.NET Core Minimal API
- Entity Framework Core 10
- SQLite
- xUnit

## Run locally

Prerequisite: .NET 10 SDK.

From the repository root:

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project SnackCounter.API
```

Use the listening URL printed by ASP.NET Core. Verify `/` and `/health`. The API creates the SQLite database using EF Core `EnsureCreated` on startup. Default connection string: `Data Source=snackcounter.db`.

## Business rules represented in the domain

- Oslo monthly budget: **6,000 NOK**.
- Bengaluru monthly budget: **15,000 INR**.
- Budgets are office-local and currency-specific. NOK and INR must not be added directly; consolidated reporting needs a selected reporting currency and dated exchange rates.
- Only available volunteers belonging to the refill's home office are eligible.
- Assignment acceptance window: **one hour**.
- Purchase follows assignment acceptance in the refill state model; reimbursement follows purchase.

## Architecture

- `SnackCounter.Domain` — entities and business invariants.
- `SnackCounter.Application` — interfaces and services.
- `SnackCounter.Infrastructure` — EF Core persistence and simulated external adapters.
- `SnackCounter.API` — ASP.NET Core host and dependency registration.
- `SnackCounter.Tests` — xUnit tests.

See [DESIGN.md](DESIGN.md) for lifecycle, decisions and remaining work.

## Known technical debt

The last observed NuGet build reported vulnerability advisories affecting dependencies including `SQLitePCLRaw.lib.e_sqlite3`, `Microsoft.OpenApi`, `System.Security.Cryptography.Pkcs` and `System.Security.Cryptography.Xml`. Review and upgrade to patched compatible versions before production; do not suppress the warnings without resolving the dependency chain.

## Next steps

1. Add transactional refill orchestration and HTTP endpoints.
2. Add low-stock detection and duplicate-open-refill protection.
3. Implement purchase/receipt validation, inventory update and budget-reservation commit as one consistent workflow.
4. Add durable chat delivery and HR claim idempotency, retries and status reconciliation.
5. Add database concurrency tests for simultaneous assignment/purchase attempts.
6. Add monthly office reports with an explicit FX conversion policy.
7. Replace startup `EnsureCreated` with EF Core migrations and add authentication/authorization.
