# BillerJacket
**By Casey Spaulding**

**Billing & Accounts Receivable service built with .NET 10, Azure Service Bus, and SQL Server.**

BillerJacket handles invoices, payments, dunning/collections, and auditability for trades and service businesses. It operates as a standalone service with a clean API boundary -- consuming applications send commands and query billing state, but never own money logic.

> **BillerJacket owns money. The consuming app owns work.**

---

## Why This Exists

Trades businesses (contractors, HVAC, plumbing, commercial services) constantly chase money. Invoices are job-based, payments are delayed or partial, and owners spend hours following up. BillerJacket automates the "who owes me, how do I collect, and what happened" problem.

This project demonstrates production-grade patterns for a fintech-adjacent SaaS service: safe money handling, idempotent payment processing, async messaging, multi-tenancy, and full auditability.

---

## Architecture

```
Consuming App (e.g. RoofingJacket)
        |
        | POST /api/invoices, /api/payments
        v
+------------------+       +------------------+       +------------------+
|  BillerJacket    | ----> |  Azure Service   | ----> |  BillerJacket    |
|  API             |       |  Bus             |       |  Worker          |
+------------------+       +------------------+       +------------------+
        |                                                      |
        v                                                      v
+--------------------------------------------------------------+
|                     Azure SQL Database                        |
|              (single source of truth for money)               |
+--------------------------------------------------------------+
        |
        v
+------------------+
|  BillerJacket    |
|  Web (Razor)     |
|  Admin Dashboard |
+------------------+
```

### Service Boundary

- **API** -- Inbound commands from consuming applications (create invoice, record payment, trigger dunning). Stateless. Publishes to Service Bus for async processing.
- **Worker** -- Consumes Service Bus queues. Processes payments, evaluates dunning plans, sends emails, normalizes webhooks. One `BackgroundService` per queue.
- **Web** -- Razor Pages admin UI for operators. Dashboard, invoice management, support tooling (DLQ viewer, webhook replay, activity log).

No shared databases or domain models between BillerJacket and consuming applications. Integration happens through well-defined API contracts.

---

## Tech Stack

| Layer | Choice |
|-------|--------|
| Runtime | .NET 10 |
| Web UI | Razor Pages (ASP.NET Core) |
| API | ASP.NET Core Web API |
| Data Access (Writes) | EF Core (code-first, migrations) |
| Data Access (Reads) | Dapper (reporting/dashboard queries) |
| Database | SQL Server (Azure SQL in production) |
| Messaging | Azure Service Bus (4 queues, dead-letter support) |
| Auth (Web) | ASP.NET Identity + cookie auth |
| Auth (API) | API key validation middleware |
| Secrets | Azure Key Vault + Managed Identity |
| Logging | Serilog (structured, correlated) |
| Observability | Application Insights + Log Analytics |
| Infrastructure | Azure CLI scripts (App Service, SQL, Service Bus, Key Vault) |

---

## Project Structure

```
src/
  BillerJacket.Web/              Razor Pages -- admin dashboard, support tools
  BillerJacket.Api/              Web API -- service boundary for consuming apps
  BillerJacket.Worker/           Background processing -- queue consumers
  BillerJacket.Application/      Cross-cutting concerns -- Current context, logging
  BillerJacket.Domain/           Entities, enums, domain invariants
  BillerJacket.Infrastructure/   EF Core context, configurations, Dapper queries
  BillerJacket.Contracts/        Message contracts shared by API + Worker
docs/
  product-model.md               What BillerJacket is and who it's for
  workflows.md                   Invoice -> Payment -> Dunning -> Webhook flows
  database.md                    Schema reference with indexes
  architecture.md                Azure deployment, messaging, auth
  support-playbook.md            Operational runbook with KQL queries
scripts/
  infra-create.sh                Azure resource provisioning
  infra-destroy.sh               Environment teardown
```

### Layer Rules

- **Web** and **API** contain no business logic
- **Application** coordinates workflows and cross-cutting concerns
- **Domain** enforces invariants (entities, enums, money rules)
- **Infrastructure** handles persistence and external services
- **Contracts** is the shared interface between publisher (API) and consumer (Worker)

---

## Domain Model

```
Tenant
  +-- Users
  +-- Customers
  |     +-- Invoices
  |          +-- Line Items
  |          +-- Payments
  |          +-- Payment Attempts
  |          +-- Communication Logs
  |          +-- Dunning State --> Dunning Plan --> Dunning Steps
  +-- API Keys
  +-- Idempotency Keys
  +-- Webhook Events
  +-- Audit Logs
```

17 entities modeling the full billing lifecycle. Every tenant-scoped entity carries a `TenantId` enforced by EF Core global query filters.

---

## Key Engineering Decisions

### CQRS-Lite (EF Core + Dapper)

EF Core handles transactional writes (invoice creation, payment application, state transitions) where change tracking and migration support matter. Dapper handles read-heavy reporting queries (dashboard totals, aging reports, overdue lists) where explicit SQL and predictable performance matter.

### Idempotent Payment Processing

Payment APIs require an `Idempotency-Key` header. The system stores request/response snapshots keyed by `(TenantId, Operation, KeyValue)`. Retries return the stored response without re-processing. This prevents double-charging under network failures or retry scenarios.

### Async Messaging with Envelope Pattern

All Service Bus messages implement a common `IMessage` interface and are wrapped in a `MessageEnvelope` for safe deserialization. Messages carry `TenantId`, `CorrelationId`, and external reference metadata through the entire pipeline.

**Queues:**

| Queue | Purpose |
|-------|---------|
| `email-send` | All outbound email (invoice, dunning, generic) |
| `dunning-evaluate` | Daily dunning evaluation per tenant |
| `payment-commands` | Async payment processing |
| `webhook-ingest` | Inbound webhook normalization + replay |

Dead-letter queues enabled on all. Transient failures retry automatically (up to 10 attempts). Non-transient failures dead-letter immediately with a reason code.

### Dunning Automation

Configurable per-tenant dunning plans with ordered steps (Day 0: friendly reminder, Day 3: overdue notice, Day 7: final warning). A daily evaluation job scans overdue invoices, advances the dunning state machine, and enqueues reminder emails. Dunning terminates when the invoice is paid, voided, or all steps are exhausted.

### Multi-Tenancy

Every request resolves a `TenantId` -- from claims (Web UI) or from `X-Tenant-Id` header validated against the API key (API). EF Core global query filters prevent cross-tenant data access at the ORM level. Dapper queries include `WHERE TenantId = @TenantId` explicitly.

### Correlation & Traceability

Every API request generates or propagates a `CorrelationId` that flows through Service Bus messages, worker processing, and database writes. Support staff can trace any invoice from API request through queue processing to final database state using a single ID.

### Immutable Audit Trail

All state changes (invoice transitions, payments, dunning actions, retries) write to an append-only `AuditLog` table with entity type, action, JSON payload, performing user, and correlation ID. This enables full "what happened and why" reconstruction for any billing event.

---

## API Surface

### Commands (from consuming applications)

```
POST /api/invoices              Create invoice
POST /api/invoices/{id}/send    Send invoice to customer
POST /api/payments              Record payment (requires Idempotency-Key header)
POST /api/dunning/run           Trigger dunning evaluation
```

### Webhooks

```
POST /api/webhooks/{provider}   Ingest provider webhook
POST /api/webhooks/{id}/replay  Replay a historical webhook event
```

### Required Headers

- `X-Api-Key` -- Service-to-service authentication
- `X-Tenant-Id` -- Tenant context
- `X-Correlation-Id` -- Optional, propagated or generated
- `Idempotency-Key` -- Required for payment endpoints

---

## Admin UI (Razor Pages)

| Route | Purpose |
|-------|---------|
| `/` | Dashboard (outstanding balance, overdue, paid this month) |
| `/customers` | Customer list and management |
| `/invoices` | Invoice list with status filters |
| `/invoices/{id}` | Invoice detail with full timeline |
| `/payments` | Payment list |
| `/activity` | Activity log |
| `/support/webhooks` | Webhook inspector with replay |
| `/support/dlq` | Dead-letter queue viewer |
| `/admin` | SuperAdmin platform dashboard |
| `/admin/tenants` | Tenant management |

---

## Security

- **Money precision** -- All monetary values use `decimal(18,2)`. No floats.
- **Idempotency** -- Enforced on all payment APIs with stored request/response snapshots
- **Audit logs** -- Immutable, append-only, with correlation IDs
- **Tenant isolation** -- EF Core query filters + middleware validation
- **API keys** -- Hashed storage, never stored in plain text
- **Managed Identity** -- No secrets in code for Azure deployments
- **Key Vault** -- Connection strings, API keys, and provider secrets
- **User secrets** -- Local development credentials via `dotnet user-secrets` (never committed)

---

## Azure Deployment

All resources provisioned via Azure CLI scripts in `scripts/`:

```
Resource Group         rg-billerjacket-{env}
App Service Plan       plan-billerjacket-{env}
Web App (UI)           app-billerjacket-web-{env}
Web App (API)          app-billerjacket-api-{env}
Web App (Worker)       app-billerjacket-worker-{env}
SQL Server             sql-billerjacket-{env}
SQL Database           sqldb-billerjacket-{env}
Service Bus            sb-billerjacket-{env}
Key Vault              kv-billerjacket-{env}
App Insights           ai-billerjacket-{env}
Log Analytics          law-billerjacket-{env}
```

CI/CD via GitHub Actions: build + test on every PR, deploy to Azure on merge to `main`.

---

## Local Development

### Prerequisites

- .NET 10 SDK
- Docker (for SQL Server)

### Setup

```bash
# Start SQL Server
docker compose up -d

# Set connection string (one-time per project)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=billerjacket;User Id=sa;Password=YourPassword;TrustServerCertificate=true" \
  --project src/BillerJacket.Api

# Apply migrations
dotnet ef database update --project src/BillerJacket.Infrastructure \
  --startup-project src/BillerJacket.Api

# Run
dotnet run --project src/BillerJacket.Web
dotnet run --project src/BillerJacket.Api
dotnet run --project src/BillerJacket.Worker
```

---

## Documentation

| Document | Description |
|----------|-------------|
| [Product Model](docs/product-model.md) | What BillerJacket is, target market, feature scope |
| [Workflows](docs/workflows.md) | Invoice, payment, dunning, and webhook flows |
| [Database Schema](docs/database.md) | Full schema reference with recommended indexes |
| [Architecture](docs/architecture.md) | Azure layout, messaging, auth, CI/CD |
| [Support Playbook](docs/support-playbook.md) | Operational runbook, tracing, DLQ management |

---

## License

Proprietary. All rights reserved.
