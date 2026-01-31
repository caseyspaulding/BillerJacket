# BillerJacket -- Architecture

## System Boundary

```
+-------------------+          +-------------------+
|  RoofingJacket    |  API     |  BillerJacket     |
|  (owns work)      | ------> |  (owns money)     |
|                   | <------ |                   |
+-------------------+  Query   +-------------------+
```

BillerJacket is a standalone billing and AR service. It does not share databases or domain models with consuming applications. Integration happens through well-defined API contracts.

---

## Solution Structure

```
BillerJacket.sln
+-- src/
    +-- BillerJacket.Web/              # Razor Pages (admin / dashboard)
    +-- BillerJacket.Api/              # Web API (service boundary)
    +-- BillerJacket.Worker/           # Background processing (queue consumers)
    +-- BillerJacket.Application/      # Use cases / orchestration / cross-cutting
    +-- BillerJacket.Domain/           # Billing domain (entities, enums, invariants)
    +-- BillerJacket.Infrastructure/   # EF Core, Azure integrations, Dapper
    +-- BillerJacket.Contracts/        # DTOs + message contracts (shared)
    +-- BillerJacket.DbMCP/            # Database MCP server (dev tooling)
```

### Layer Rules

- **Web** and **Api** contain no business logic
- **Application** coordinates workflows
- **Domain** enforces invariants
- **Infrastructure** talks to Azure services and databases
- **Contracts** is shared between Api (publisher) and Worker (consumer)

---

## Azure Deployment Architecture

### Compute

| Service | Hosts |
|---------|-------|
| Azure App Service | BillerJacket.Web, BillerJacket.Api, BillerJacket.Worker |

All three run on a single App Service Plan (`B1` for dev, scale as needed).

### Data

| Service | Purpose |
|---------|---------|
| Azure SQL Database | Single source of truth for money |

### Messaging

| Service | Queues |
|---------|--------|
| Azure Service Bus (Standard) | `email-send`, `dunning-evaluate`, `payment-commands`, `webhook-ingest` |

Dead-letter queues enabled on all. Max delivery count: 10.

### Security & Ops

| Service | Purpose |
|---------|---------|
| Azure Key Vault | Secrets management (connection strings, API keys) |
| Managed Identity | No secrets in code -- App Services authenticate to Key Vault via identity |
| Application Insights | Traces, metrics, exceptions |
| Log Analytics Workspace | Central logs, KQL queries, SQL auditing |

### Optional (Later)

| Service | Purpose |
|---------|---------|
| Microsoft Entra ID | SSO |
| Azure Functions | Timers / orchestration |
| Azure API Management | Policies, rate limits, versioning |
| Azure Front Door | WAF + edge routing |
| Azure Communication Services | Email / SMS sending |

---

## Naming Convention

```
Resource Group:    rg-billerjacket-{env}
App Service Plan:  plan-billerjacket-{env}
Web App (UI):      app-billerjacket-web-{env}
Web App (API):     app-billerjacket-api-{env}
Web App (Worker):  app-billerjacket-worker-{env}
SQL Server:        sql-billerjacket-{env}
SQL Database:      sqldb-billerjacket-{env}
Service Bus:       sb-billerjacket-{env}
Key Vault:         kv-billerjacket-{env}
App Insights:      ai-billerjacket-{env}
Log Analytics:     law-billerjacket-{env}
```

Where `{env}` = `dev`, `staging`, or `prod`.

---

## Data Access Strategy (CQRS-Lite)

### Write Side: EF Core

- Invoice / payment creation
- Idempotency key enforcement
- State transitions
- Audit log writes
- Transactional consistency
- Code-first migrations

### Read Side: Dapper

- Dashboard totals
- Overdue invoice lists
- Customer aging reports
- "Who owes me money" rollups
- Exports

### Why Hybrid

Write side stays safe and maintainable (EF Core tracks changes, enforces constraints). Read side stays fast and explicit (raw SQL, predictable performance).

---

## Authentication & Multi-Tenancy

### Two Auth Surfaces

| Surface | Mechanism | Consumer |
|---------|-----------|----------|
| Web UI (Razor Pages) | ASP.NET Identity + cookie auth | Human operators |
| API (Web API) | API key in `X-Api-Key` header | RoofingJacket, future verticals |

### Tenant Resolution

- **Web UI:** TenantId from claims (set at login)
- **API:** TenantId from `X-Tenant-Id` header, validated against API key
- **EF Core:** Global query filter on all tenant-scoped entities
- **Dapper:** Every query includes `WHERE TenantId = @TenantId`

### Roles

| Role | Scope | Purpose |
|------|-------|---------|
| SuperAdmin | Platform-wide | Blog, pages CMS, tenant management |
| Admin | Tenant | Full access within their org |
| Finance | Tenant | Invoice, payment, reporting |
| Support | Tenant | Read-only + replay/DLQ tooling |

---

## Messaging Architecture

### Queue Layout

| Queue | Purpose | Publisher | Consumer |
|-------|---------|-----------|----------|
| `email-send` | All outbound email | API, Dunning Worker | Email Worker |
| `dunning-evaluate` | Daily dunning evaluation | Scheduler | Dunning Worker |
| `payment-commands` | Async payment processing | API | Payment Worker |
| `webhook-ingest` | Inbound webhook normalization | API | Webhook Worker |

### Message Flow

```
API Request
    |
    v
[Validate + Store in SQL]
    |
    v
[Publish to Service Bus]
    |
    v
Worker consumes
    |
    v
[Process + Write results to SQL]
    |
    v
[Complete or Dead-letter message]
```

### Message Contract Structure

All messages implement `IMessage` with: `MessageType`, `TenantId`, `CorrelationId`, `ExternalSource`, `ExternalReferenceId`, `RequestedByUserId`, `OccurredAt`.

Messages are wrapped in a `MessageEnvelope` for safe deserialization:

```
MessageEnvelope
  MessageType: "payment.apply"
  PayloadJson: "{...serialized command...}"
  TenantId: "<guid>"
  CorrelationId: "<guid>"
  EnqueuedAt: "2025-01-15T..."
```

### Worker Structure

One `BackgroundService` per queue:

```
Worker/
  Messaging/
    PaymentsProcessorHostedService.cs    # payment-commands
    DunningProcessorHostedService.cs     # dunning-evaluate
    WebhookProcessorHostedService.cs     # webhook-ingest
    EmailProcessorHostedService.cs       # email-send
```

### Retry & DLQ Strategy

| Scenario | Action |
|----------|--------|
| Transient failure | Abandon message (Service Bus retries up to MaxDeliveryCount) |
| Invalid payload / bad envelope | Dead-letter immediately |
| Unknown message type | Dead-letter immediately |
| MaxDeliveryCount exceeded | Service Bus auto-DLQs |

---

## Correlation & Observability

### Correlation Propagation

| Layer | Responsibility |
|-------|----------------|
| API | Read `X-Correlation-Id` header or generate new |
| Publisher | Set `ServiceBusMessage.CorrelationId` + application properties |
| Worker | Start logging scope with `tenantId` + `correlationId` |
| DB writes | Write `CorrelationId` into domain rows |

### LoggingContext

Every handler uses structured logging context:

```csharp
using var _ = _logging.WithContext(
    feature: "Payment",
    operation: "ApplyPayment",
    component: "API",
    organizationId: tenantId
);
```

This enables feature-level grouping in Application Insights and reduces debugging token usage.

---

## Security Defaults

- Decimal-only money (`decimal(18,2)`)
- Idempotency keys enforced on payment APIs
- Immutable audit logs
- Managed Identity everywhere (no secrets in code)
- Least-privilege RBAC
- API keys hashed (never stored plain)
- Correlation IDs across API -> queue -> worker -> DB
- Tenant isolation via query filters + middleware validation

---

## CI/CD (GitHub Actions)

### `ci.yml` -- Build + Test (every push / PR)

1. Checkout
2. Setup .NET 10
3. `dotnet restore`
4. `dotnet build --no-restore`
5. `dotnet test --no-build`
6. Fail the PR if any step fails

### `deploy.yml` -- Deploy to Azure (on merge to `main`)

1. Run full CI steps
2. `dotnet publish` for Web, Api, Worker
3. Deploy to Azure App Service
4. Run EF Core migrations against Azure SQL

### Environments

| Environment | Trigger | Purpose |
|-------------|---------|---------|
| Dev / Staging | Push to `main` | Validate in Azure |
| Production | Manual approval or tag | Ship when ready |

---

## Infrastructure Provisioning

Azure resources provisioned via `az` CLI scripts in `scripts/`:

| Script | Purpose |
|--------|---------|
| `infra-create.sh` | Full environment setup (run once) |
| `infra-destroy.sh` | Tear down everything (dev cleanup) |

No Terraform/Bicep for v1. Scripts are readable, copy-pasteable, and in the repo.

---

## Integration with RoofingJacket

### Commands (RoofingJacket -> BillerJacket)

- `POST /api/invoices` -- Create invoice
- `POST /api/invoices/{id}/send` -- Send invoice
- `POST /api/payments` -- Record payment (idempotent)

### Queries (RoofingJacket reads from BillerJacket)

- `BillingSummary` -- Total owed, overdue, paid
- `InvoiceStatus` -- Current state of an invoice
- `PaymentHistory` -- Payments for an invoice

RoofingJacket displays billing state but **never mutates billing data**.
