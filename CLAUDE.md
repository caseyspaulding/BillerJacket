# BillerJacket - Technical Spec for Claude Code

## What We're Building

**BillerJacket** is a Billing & Accounts Receivable (AR) automation service.
**Category:** Invoice management, payment tracking, dunning/collections automation.

> Full product plan, market analysis, and SEO research are in `docs/BillerJacket-Plan.md`.
> Operational runbook is in `docs/support-playbook.md`.

---

## Build Status

> **Stage: Foundation Complete -- Ready for Feature Development**

### What's Built
- [x] .NET 10 solution structure (8 projects)
- [x] Domain entities (17 entities + 9 enums)
- [x] EF Core write context (ArDbContext) with tenant query filters
- [x] EF Core entity configurations (14 configuration classes)
- [x] ASP.NET Identity context (AppIdentityDbContext)
- [x] Service Bus message contracts (6 message types)
- [x] Bus publisher infrastructure (IBusPublisher)
- [x] Application common layer (Current, LoggingContext, SystemUser)
- [x] Web project skeleton (Razor Pages + Identity + auth policies)
- [x] API project skeleton (controllers + health check)
- [x] Worker project skeleton (hosted service pattern)
- [x] DbMCP server for LLM database access
- [x] Azure CLI infra scripts (create + destroy)

### What's NOT Built (V1 TODO)
- [ ] API controllers (invoices, payments, webhooks, dunning)
- [ ] Razor Pages (dashboard, customers, invoices, payments, activity, support tools, admin)
- [ ] Worker queue processors (email, dunning, payments, webhooks)
- [ ] CustomClaimsPrincipalFactory (bridges Identity to User table)
- [ ] API key middleware (service-to-service auth)
- [ ] SetupRedirectMiddleware
- [ ] Dapper reporting queries (dashboard, aging)
- [ ] EF Core migrations (initial migration not yet created)

---

## Core Principle

> **BillerJacket owns money. RoofingJacket owns work.**

No shared databases. No shared domain models. Only well-defined service contracts.

---

## Tech Stack

| Layer | Choice | Notes |
|-------|--------|-------|
| Runtime | .NET 10 | |
| Web Framework | Razor Pages (Web) + Controllers (API) | ASP.NET Core 10 |
| Data Access (Writes) | EF Core | Code-first, migrations, ArDbContext |
| Data Access (Reads) | Dapper | Reporting queries only |
| Database | SQL Server | Docker (dev), Azure SQL (prod) |
| Messaging | Azure Service Bus | 4 queues, envelope pattern |
| Auth (Web) | ASP.NET Identity + cookie auth | |
| Auth (API) | API key in X-Api-Key header | |
| Logging | Serilog | Structured, to console |
| Secrets | Azure Key Vault + Managed Identity | |
| Observability | Application Insights + Log Analytics | |
| Dev Tools | BillerJacket.DbMCP | LLM-accessible DB server |

> **EF Core is used for ALL application tables** (unlike RoofingJacket which uses Dapper for app tables). Dapper is only for read-heavy reporting/dashboard queries.

---

## Project Structure

```
BillerJacket.sln
+-- src/
    +-- BillerJacket.Web/              # Razor Pages (admin / dashboard / human UI)
    +-- BillerJacket.Api/              # Web API (service boundary for RoofingJacket)
    +-- BillerJacket.Worker/           # Background processing (queue consumers)
    +-- BillerJacket.Application/      # Cross-cutting: Current, LoggingContext, SystemUser
    +-- BillerJacket.Domain/           # Entities, enums, domain invariants
    +-- BillerJacket.Infrastructure/   # EF Core (ArDbContext, configurations), Identity, Dapper
    +-- BillerJacket.Contracts/        # Message contracts (shared by API + Worker)
    +-- BillerJacket.DbMCP/            # Database MCP server (dev tooling)
+-- docs/                             # Product plan, architecture, workflows, schema, playbook
+-- scripts/                          # Azure CLI infra provisioning
```

### Layer Rules

- **Web** and **Api** contain no business logic
- **Application** coordinates workflows and provides cross-cutting concerns
- **Domain** enforces invariants (entities, enums, value objects)
- **Infrastructure** talks to Azure services and databases
- **Contracts** is shared between Api (publisher) and Worker (consumer)

---

## Architectural Patterns

### 1. Current Context (Static Accessor)

`Application/Common/Current.cs` provides request-scoped static access:

```csharp
Current.TenantId              // Guid -- throws if missing
Current.TenantIdOrNull        // Guid? -- safe access
Current.UserId                // Guid from claims
Current.Email                 // string
Current.Role                  // string (SuperAdmin, Admin, Finance, Support)
Current.IsAuthenticated       // bool
Current.IsSuperAdmin          // bool
Current.CorrelationId         // string (TraceIdentifier or generated)
```

Initialized in Program.cs: `Current.Initialize(app.Services.GetRequiredService<IHttpContextAccessor>());`

### 2. Multi-Tenancy

- `TenantId` on all tenant-scoped entities
- EF Core global query filters in ArDbContext (applied when TenantId constructor param is set)
- Dapper queries must always include `WHERE TenantId = @TenantId`
- Web UI: TenantId from claims (set at login by ClaimsPrincipalFactory)
- API: TenantId from `X-Tenant-Id` header, validated against API key
- BlogPost and LandingPage are global (no TenantId) -- SuperAdmin only

### 3. CQRS-Lite (EF Core + Dapper)

- **Writes:** EF Core via ArDbContext (transactions, change tracking, migrations)
- **Reads:** Dapper in `Infrastructure/Reporting/` for dashboard and aging queries

```
Infrastructure/
  Data/
    ArDbContext.cs
    Configurations/          # 14 IEntityTypeConfiguration<T> classes
  Reporting/
    InvoiceDashboardQueries.cs   # Dapper
    CustomerAgingQueries.cs      # Dapper
```

### 4. Service Bus Messaging

**Queues:** `email-send`, `dunning-evaluate`, `payment-commands`, `webhook-ingest`

**Message flow:** All messages implement `IMessage`, wrapped in `MessageEnvelope`:
```csharp
// Publishing (API side)
await _bus.PublishAsync(Queues.PaymentCommands, new ApplyPaymentCommand(...));

// Consuming (Worker side) -- one BackgroundService per queue
Worker/
  Messaging/
    PaymentsProcessorHostedService.cs
    DunningProcessorHostedService.cs
    WebhookProcessorHostedService.cs
    EmailProcessorHostedService.cs
```

**Message types:**
- `ApplyPaymentCommand` -> `payment-commands`
- `InvoiceEmailRequested` -> `email-send`
- `DunningEmailRequested` -> `email-send`
- `EvaluateDunningCommand` -> `dunning-evaluate`
- `WebhookReceived` -> `webhook-ingest`
- `WebhookReplayRequested` -> `webhook-ingest`

**Error handling:**
- Transient failure: `AbandonMessageAsync` (Service Bus retries)
- Invalid payload: `DeadLetterMessageAsync` immediately
- Unknown message type: `DeadLetterMessageAsync` immediately
- After MaxDeliveryCount (10): auto-DLQ

### 5. Structured Logging (LoggingContext)

**MANDATORY for all new code** (see global CLAUDE.md for full requirements):

```csharp
using var _ = _logging.WithContext(
    feature: "Payment",
    operation: "ApplyPayment",
    component: "API",           // API | Worker | SignalR
    tenantId: tenantId,         // optional
    jobId: jobId                // optional, for worker jobs
);
```

Note: BillerJacket uses `component: "Worker"` (not "Hangfire") for background processing.

### 6. Correlation IDs

Every flow propagates a `CorrelationId`:
- **API:** Read `X-Correlation-Id` header or generate new
- **Publisher:** Set on `ServiceBusMessage.CorrelationId` + application properties
- **Worker:** Start logging scope with `tenantId` + `correlationId` from envelope
- **DB writes:** Write `CorrelationId` into domain rows (Payment, PaymentAttempt, WebhookEvent, CommunicationLog, AuditLog)

---

## Domain Model

### Core Entities

| Entity | Key | Tenant-Scoped |
|--------|-----|---------------|
| Tenant | TenantId | No (is the tenant) |
| User | UserId | Yes |
| Customer | CustomerId | Yes |
| Invoice | InvoiceId | Yes |
| InvoiceLineItem | InvoiceLineItemId | Yes |
| Payment | PaymentId | Yes |
| PaymentAttempt | PaymentAttemptId | Yes |
| IdempotencyKey | IdempotencyKeyId | Yes |
| DunningPlan | DunningPlanId | Yes |
| DunningStep | DunningStepId | Yes |
| InvoiceDunningState | InvoiceId (PK+FK) | Yes |
| CommunicationLog | CommunicationLogId | Yes |
| WebhookEvent | WebhookEventId | Yes |
| AuditLog | AuditLogId | Yes |
| ApiKeyRecord | ApiKeyId | Yes |
| BlogPost | BlogPostId | No (global) |
| LandingPage | LandingPageId | No (global) |

### Enums

`InvoiceStatus`, `PaymentStatus`, `PaymentMethod`, `CommunicationChannel`, `CommunicationType`, `CommunicationStatus`, `WebhookProcessingStatus`, `ContentStatus`, `PageType`

### Money Rules

- All money columns: `decimal(18,2)` -- no floats, no doubles
- `Invoice.BalanceDue` is a computed C# property (`TotalAmount - PaidAmount`), ignored by EF Core
- `InvoiceLineItem.LineTotal` is computed in domain (`Quantity * UnitPrice`)
- Idempotency keys required on payment APIs
- Audit logs are immutable (append-only)

---

## Authentication & Authorization

### Web UI (Razor Pages)

- ASP.NET Identity with cookie auth
- Password: 8+ chars, upper, lower, digit (no special char required)
- 8-hour sliding expiration
- Login: `/login`, Logout: `/logout`

### API (Service-to-Service)

- `X-Api-Key` header validated by middleware
- `X-Tenant-Id` header required, validated against API key's tenant
- Keys stored hashed in `ApiKeyRecord` table

### Roles

| Role | Scope | Access |
|------|-------|--------|
| SuperAdmin | Platform | Blog, pages, tenant management (no TenantId) |
| Admin | Tenant | Full access within their org |
| Finance | Tenant | Invoices, payments, reporting |
| Support | Tenant | Read-only + replay/DLQ tooling |

### Authorization Policies

```csharp
options.AddPolicy("AdminOnly", p => p.RequireClaim("role", "Admin"));
options.AddPolicy("SuperAdmin", p => p.RequireClaim("role", "SuperAdmin"));
```

Razor Pages folder conventions:
- `/` -- requires auth (all pages)
- `/Login`, `/Index` -- anonymous allowed
- `/Admin/*` -- SuperAdmin policy

### Claims

| Claim | Source |
|-------|--------|
| `user_id` | User.UserId |
| `tenant_id` | User.TenantId |
| `role` | User.Role |
| `email` | User.Email |

---

## API Design

### Inbound Commands

| Method | Route | Purpose | Auth |
|--------|-------|---------|------|
| POST | `/api/invoices` | Create invoice | API key |
| POST | `/api/invoices/{id}/send` | Send invoice | API key |
| POST | `/api/payments` | Record payment (idempotent) | API key + Idempotency-Key header |
| POST | `/api/dunning/run` | Trigger dunning evaluation | API key |

### Webhooks

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/api/webhooks/{provider}` | Ingest provider webhook |
| POST | `/api/webhooks/{id}/replay` | Replay a webhook event |

### Headers

- `X-Api-Key` -- required for all API endpoints
- `X-Tenant-Id` -- required, identifies the tenant
- `X-Correlation-Id` -- optional, propagated or generated
- `Idempotency-Key` -- required for payment APIs

---

## Razor Pages (Web UI)

| Route | Purpose |
|-------|---------|
| `/login` | Authentication |
| `/` | Dashboard |
| `/customers` | Customer list |
| `/invoices` | Invoice list |
| `/invoices/{id}` | Invoice detail + timeline |
| `/payments` | Payment list |
| `/activity` | Activity log |
| `/support/webhooks` | Webhook inspector |
| `/support/dlq` | Dead-letter queue viewer |
| `/admin` | SuperAdmin dashboard |
| `/admin/blog` | Blog post list |
| `/admin/blog/edit/{id?}` | Blog post editor |
| `/admin/pages` | Landing page list |
| `/admin/pages/edit/{id?}` | Landing page editor |
| `/admin/tenants` | Tenant list |
| `/admin/tenants/edit/{id}` | Tenant editor |

---

## EF Core Conventions

### Entity Configuration Pattern

Each entity has a dedicated configuration in `Infrastructure/Data/Configurations/`:

```csharp
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(e => e.InvoiceId);
        builder.Property(e => e.SubtotalAmount).HasColumnType("decimal(18,2)");
        // ... indexes, relationships, constraints
    }
}
```

### Key Conventions

- All PKs are `Guid` (no auto-increment)
- Enum properties stored as strings: `.HasConversion<string>()`
- Money columns: `.HasColumnType("decimal(18,2)")`
- Computed domain properties: `.Ignore(e => e.BalanceDue)`
- Delete behavior: `DeleteBehavior.Restrict` on tenant FKs
- Configurations auto-discovered: `modelBuilder.ApplyConfigurationsFromAssembly(...)`

### Migrations

- Code-first via `dotnet ef migrations add <Name>`
- Applied during deploy via `dotnet ef database update`
- Never applied manually in production
- Migration project: `BillerJacket.Infrastructure`
- Startup project: `BillerJacket.Api` (for migration commands)

---

## Worker Pattern

One `BackgroundService` per queue. Each processor:
1. Deserializes `MessageEnvelope`
2. Routes by `MessageType` string
3. Deserializes concrete command from `PayloadJson`
4. Handles with explicit Complete/Abandon/DeadLetter
5. Uses logging scope with `tenantId` + `correlationId`

---

## SystemUser

Well-known identity for automated actions (dunning jobs, worker processing):

```csharp
public static class SystemUser
{
    public static readonly Guid Id = new("00000000-0000-0000-0000-000000575753");
    public const string Name = "System";
    public const string Email = "system@billerjacket.com";
}
```

Used in `AuditLog.PerformedByUserId` and `Payment.CreatedByUserId` for automated operations.

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker (for SQL Server dev container)

### Setup

```bash
# Start SQL Server dev container (if using docker-compose)
docker compose up -d

# Run the web app
cd src/BillerJacket.Web
dotnet run

# Run the API
cd src/BillerJacket.Api
dotnet run

# Run the worker
cd src/BillerJacket.Worker
dotnet run

# Run the DbMCP server (for LLM DB access)
cd src/BillerJacket.DbMCP
dotnet run
```

### Connection Strings

Configured in `appsettings.json` / `appsettings.Development.json`:
- `DefaultConnection` -- SQL Server (used by ArDbContext + AppIdentityDbContext)
- `ServiceBus` -- Azure Service Bus (optional for local dev)

### Health Checks

- Web: `GET /health`
- API: `GET /health`

---

## Azure Infrastructure

### Naming Convention

```
rg-billerjacket-{env}           # Resource Group
plan-billerjacket-{env}         # App Service Plan
app-billerjacket-web-{env}      # Web App (UI)
app-billerjacket-api-{env}      # Web App (API)
app-billerjacket-worker-{env}   # Web App (Worker)
sql-billerjacket-{env}          # SQL Server
sqldb-billerjacket-{env}        # SQL Database
sb-billerjacket-{env}           # Service Bus
kv-billerjacket-{env}           # Key Vault
ai-billerjacket-{env}           # App Insights
law-billerjacket-{env}          # Log Analytics
```

Where `{env}` = `dev`, `staging`, or `prod`.

### Provisioning

Azure resources provisioned via scripts in `scripts/`:
- `infra-create.sh` -- Full environment setup
- `infra-destroy.sh` -- Tear down everything

---

## Feature Phasing

**V1 (Current Target)**
- Customers, invoices (one-time + installments), manual payments (idempotent)
- Dunning (email reminders), activity log, dashboard
- SuperAdmin administration (tenants, blog, pages)

**V1.5**
- Autopay, webhook replay UI, aging reports

**V2**
- Multiple currencies, accounting sync, advanced dunning, payment provider integrations

---

## Key Documentation

| File | Purpose |
|------|---------|
| `docs/BillerJacket-Plan.md` | Full product plan, ERD, messaging guide, auth spec |
| `docs/product-model.md` | What BillerJacket is, target market, scope |
| `docs/workflows.md` | Invoice -> Payment -> Dunning -> Webhook flows |
| `docs/database.md` | Schema reference, indexes, migration strategy |
| `docs/support-playbook.md` | Operational runbook, tracing, DLQ, KQL queries |
| `docs/architecture.md` | Azure layout, solution structure, messaging, CI/CD |
