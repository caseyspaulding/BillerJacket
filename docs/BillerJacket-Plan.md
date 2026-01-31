# BillerJacket -- Billing & AR Service (Final Plan)

## Product Intent

BillerJacket is an Accounts Receivable (AR) automation service that handles:

- Invoices
- Payments
- Autopay (future)
- Dunning / collections
- Auditability and support tooling

It is designed to operate as a standalone internal service consumed by:

- **RoofingJacket** (initial client)
- Future vertical products (HVACJacket, ServiceJacket, etc.)

---

## Target Market & Use Cases

BillerJacket is purpose-built for trades and service businesses where invoices are job-based, payments are delayed or partial, customers don't pay immediately, and owners hate chasing money.

### Trades with Milestone / Progress Billing (Best Fit)

These businesses don't get paid all at once and constantly chase money.

- **General Contractors** -- Deposit, rough-in, inspections, final. Retainage is common. Subs + change orders complicate billing. *Top-tier fit.*
- **Concrete & Hardscape Contractors** -- Driveways, patios, foundations. Weather delays cause billing delays. Progress payments + final balance. *Excellent fit.*
- **Window / Door / Siding Installers** -- $15k--$60k jobs. Payment plans common. Final payment often slow. *Excellent fit.*
- **Remodelers (kitchens, baths, additions)** -- Change orders constantly. Clients dispute "what's included." Invoices tied to job stages. *Excellent fit.*

### Trades with Service + Project Hybrid Billing

These are sneaky-good markets.

- **HVAC Companies** -- Maintenance subscriptions, equipment installs, financing + staged payments, lots of AR complexity. *Very strong fit.*
- **Plumbing (especially commercial)** -- Emergency work (paid fast), renovation + commercial work (paid slow), NET terms common. *Strong fit.*
- **Electrical Contractors** -- Commercial jobs, PO-based invoicing, NET 30 / NET 60 / NET 90. *Strong fit.*

### Commercial & B2B Service Trades (Often Overlooked, Very Profitable)

- **Commercial Cleaning Companies** -- Monthly invoices, clients pay late constantly, owners spend hours chasing checks. *Excellent fit.*
- **Elevator / Mechanical Service Companies** -- Long-term service contracts, recurring + project billing, extremely AR-heavy. *Excellent fit.*
- **Industrial Maintenance / Field Services** -- Work orders to invoices, large corporate customers, slow payment cycles. *Excellent fit.*

### Construction-Adjacent Specialties

- **Fire Protection / Sprinkler Systems** -- Compliance-driven inspections, invoices tied to reports, recurring + one-time billing. *Very strong fit.*
- **Steel / Structural Fabricators** -- Progress billing, long lead times, retainage. *Strong fit.*

### High-Ticket Consumer Services (Often Ignored, Huge Pain)

- **Solar Installers** -- Permitting delays, utility coordination, financing + progress billing, long AR cycles. *Excellent fit.*
- **Fence Companies (large installs)** -- Multi-day installs, partial payments, customers disappear after install. *Good fit.*
- **Paving / Asphalt Contractors** -- Municipal + commercial clients, slow payers, invoices tied to completion milestones. *Strong fit.*

### Where This Doesn't Fit (Important to Know)

These usually don't have enough AR pain:

- Landscapers (pay-on-completion)
- Handymen
- Pressure washing
- Solo operators with small tickets
- Cash-first trades

Knowing where not to go is a strength.

### The Pattern

The best fits all share these traits:

1. Invoices are job-based
2. Payments are delayed or partial
3. Customers don't pay immediately
4. QuickBooks is already used
5. Owners hate chasing money

When you see those five things together -- BillerJacket fits.

---

## SEO Keyword Research

Target keywords for blog content, landing pages, and organic search strategy. Data sourced from Ahrefs.

**Totals:** 13 keywords | 11K search volume | 28K global search volume

| Keyword | Intent | KD | SV | GSV | CPC | Parent Topic |
|---------|--------|---:|---:|----:|----:|--------------|
| accounts receivable automation | I | 12 | 1,400 | 3,400 | $0.45 | automated accounts receivable |
| accounts recievable | I | 17 | 1,200 | 1,900 | $0.60 | accounts receivable |
| ar automation | I | 11 | 1,200 | 2,600 | $25.00 | ar automation |
| accounts receivable services | I | 6 | 1,100 | 2,100 | $0.45 | accounts receivable services |
| accounts receivable solutions | I, C | 9 | 1,000 | 1,600 | $0.50 | ar solutions |
| ap ar automation | I | 2 | 800 | 2,700 | $0.60 | ap ar automation |
| accounts receivable collections software | I, C | 20 | 800 | 1,300 | $0.50 | accounts receivable software |
| automated accounts receivable | I | 11 | 800 | 2,700 | $0.45 | automated accounts receivable |
| accounts receivable system | I | 15 | 700 | 1,200 | $0.50 | accounts receivable software |
| account receivable software | I, C | 20 | 700 | 1,100 | $0.60 | accounts receivable software |
| accounts receivable management software | I, C | 19 | 700 | 2,000 | $0.40 | accounts receivable software |
| accounts receivable automation software | I, C | 17 | 600 | 3,400 | $0.45 | accounts receivable platform |
| account receivables | I | 11 | 600 | 2,000 | $0.60 | accounts receivable |

**Legend:** I = Informational, C = Commercial | KD = Keyword Difficulty | SV = US Search Volume | GSV = Global Search Volume | CPC = Cost Per Click

**Key Takeaways:**

- "accounts receivable automation" and "ar automation" are high-volume, low-difficulty (KD 11--12) -- prime blog/landing page targets
- "accounts receivable services" has the lowest KD (6) at 1,100 SV -- easiest win
- Commercial-intent keywords (solutions, software, collections software) map directly to landing pages
- "ar automation" has a $25 CPC outlier -- high commercial value, competitors are bidding aggressively
- Most keywords cluster under 3 parent topics: "accounts receivable software", "automated accounts receivable", and "ar automation"

---

## Core Architectural Principle

> **BillerJacket owns money.**
> **RoofingJacket owns work.**

- No shared databases.
- No shared domain models.
- Only well-defined service contracts.

---

## 1. Solution Structure

```
BillerJacket.sln
└─ src/
   ├─ BillerJacket.Web/              # Razor Pages (admin / dashboard)
   ├─ BillerJacket.Api/              # Web API (service boundary)
   ├─ BillerJacket.Worker/           # Background processing
   ├─ BillerJacket.Application/      # Use cases / orchestration
   ├─ BillerJacket.Domain/           # Billing domain (money rules)
   ├─ BillerJacket.Infrastructure/   # EF Core, Azure integrations
   └─ BillerJacket.Contracts/        # DTOs + message contracts
```

**Rules:**

- UI and API contain no business logic
- Application layer coordinates workflows
- Domain enforces invariants
- Infrastructure talks to Azure

---

## 2. Data Access Strategy (CQRS-Lite)

**Approach:** EF Core for writes + Dapper for reads

### EF Core (Write Side)

- Invoice / payment creation
- Idempotency key enforcement
- State transitions
- Audit log writes
- Transactional consistency
- Code-first migrations

### Dapper (Read Side)

- Dashboard totals
- Overdue invoice lists
- "Who owes me money" rollups
- Customer aging reports
- Exports

### Why Hybrid

- Write side stays safe and maintainable (EF Core tracks changes, enforces constraints)
- Read side stays fast and explicit (raw SQL, predictable performance)
- Demonstrates mature tradeoffs in a portfolio context

> "I use EF Core for transactional write workflows and migrations, and Dapper for read-heavy reporting queries where I want explicit SQL and predictable performance."

### Fintech-Specific Note

For money systems, correctness > ORM philosophy. Regardless of tool, the system must enforce:

- Transactions where needed
- Idempotency
- Audit trail
- Decimal precision
- Database constraints

### Infrastructure Layout

```
Infrastructure/
  Data/
    ArDbContext.cs              # EF Core write context
    Configurations/            # Entity type configurations
  Reporting/
    InvoiceDashboardQueries.cs # Dapper
    CustomerAgingQueries.cs    # Dapper
```

### v1 Approach

Ship with EF Core (code-first, migrations, indexes in model config). Add one Dapper `Reporting/` folder for dashboard queries to show you know when to drop down to SQL.

---

## 3. Azure-Only Deployment Architecture

### Compute

| Service | Hosts |
|---------|-------|
| Azure App Service | `BillerJacket.Web`, `BillerJacket.Api`, `BillerJacket.Worker` |

### Data

| Service | Purpose |
|---------|---------|
| Azure SQL Database | Single source of truth for money |

### Messaging

| Service | Queues |
|---------|--------|
| Azure Service Bus | `email-send`, `dunning-evaluate`, `payment-commands`, `webhook-ingest` |

Dead-letter queues enabled on all.

### Security & Ops

| Service | Purpose |
|---------|---------|
| Azure Key Vault | Secrets management |
| Managed Identity | No secrets in code |
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

### Infrastructure Provisioning (Azure CLI)

All Azure resources provisioned via `az` CLI scripts -- no Portal clicking, no Terraform/Bicep for v1. Scripts live in the repo and are runnable end-to-end.

```
scripts/
├─ infra-create.sh          # Full environment setup (run once)
├─ infra-destroy.sh         # Tear down everything (dev cleanup)
└─ infra-queues.sh          # Create/update Service Bus queues
```

#### Naming Convention

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

#### Provisioning Order

```bash
# 1. Resource Group
az group create --name rg-billerjacket-dev --location eastus

# 2. Log Analytics Workspace
az monitor log-analytics workspace create \
  --resource-group rg-billerjacket-dev \
  --workspace-name law-billerjacket-dev

# 3. Application Insights (linked to Log Analytics)
az monitor app-insights component create \
  --app ai-billerjacket-dev \
  --resource-group rg-billerjacket-dev \
  --location eastus \
  --workspace law-billerjacket-dev

# 4. Key Vault
az keyvault create \
  --name kv-billerjacket-dev \
  --resource-group rg-billerjacket-dev \
  --enable-rbac-authorization

# 5. SQL Server + Database
az sql server create \
  --name sql-billerjacket-dev \
  --resource-group rg-billerjacket-dev \
  --admin-user billeradmin \
  --admin-password <from-keyvault>

az sql db create \
  --name sqldb-billerjacket-dev \
  --server sql-billerjacket-dev \
  --resource-group rg-billerjacket-dev \
  --service-objective S0

# 6. Service Bus Namespace + Queues
az servicebus namespace create \
  --name sb-billerjacket-dev \
  --resource-group rg-billerjacket-dev \
  --sku Standard

az servicebus queue create --namespace-name sb-billerjacket-dev \
  --resource-group rg-billerjacket-dev --name email-send \
  --max-delivery-count 10 --enable-dead-lettering-on-message-expiration

az servicebus queue create --namespace-name sb-billerjacket-dev \
  --resource-group rg-billerjacket-dev --name dunning-evaluate \
  --max-delivery-count 10 --enable-dead-lettering-on-message-expiration

az servicebus queue create --namespace-name sb-billerjacket-dev \
  --resource-group rg-billerjacket-dev --name payment-commands \
  --max-delivery-count 10 --enable-dead-lettering-on-message-expiration

az servicebus queue create --namespace-name sb-billerjacket-dev \
  --resource-group rg-billerjacket-dev --name webhook-ingest \
  --max-delivery-count 10 --enable-dead-lettering-on-message-expiration

# 7. App Service Plan
az appservice plan create \
  --name plan-billerjacket-dev \
  --resource-group rg-billerjacket-dev \
  --sku B1 --is-linux

# 8. App Services (Web, API, Worker)
az webapp create --name app-billerjacket-web-dev \
  --resource-group rg-billerjacket-dev \
  --plan plan-billerjacket-dev --runtime "DOTNETCORE:10.0"

az webapp create --name app-billerjacket-api-dev \
  --resource-group rg-billerjacket-dev \
  --plan plan-billerjacket-dev --runtime "DOTNETCORE:10.0"

az webapp create --name app-billerjacket-worker-dev \
  --resource-group rg-billerjacket-dev \
  --plan plan-billerjacket-dev --runtime "DOTNETCORE:10.0"

# 9. Enable Managed Identity on each App Service
az webapp identity assign --name app-billerjacket-web-dev \
  --resource-group rg-billerjacket-dev
az webapp identity assign --name app-billerjacket-api-dev \
  --resource-group rg-billerjacket-dev
az webapp identity assign --name app-billerjacket-worker-dev \
  --resource-group rg-billerjacket-dev

# 10. Grant Key Vault access to each identity
# (use object IDs from step 9 output)
az role assignment create --role "Key Vault Secrets User" \
  --assignee <web-principal-id> \
  --scope /subscriptions/<sub>/resourceGroups/rg-billerjacket-dev/providers/Microsoft.KeyVault/vaults/kv-billerjacket-dev
```

#### Key Vault Secrets to Store

| Secret Name | Value |
|-------------|-------|
| `SqlConnectionString` | Azure SQL connection string |
| `ServiceBusConnectionString` | Service Bus connection string |
| `AppInsightsConnectionString` | App Insights instrumentation |

#### Why Azure CLI (Not Terraform/Bicep for v1)

- Immediate, readable, copy-pasteable
- No state file to manage
- Easy to explain in an interview
- Scripts in the repo = reproducible
- Upgrade to Bicep later if the project earns it

---

### Queue & Message Naming Conventions

- Queue names use kebab-case (e.g. `email-send`, `payment-commands`)
- Message type strings use dot notation (e.g. `email.invoice_requested`, `payment.apply`)

All messages include:

```
TenantId
CorrelationId
ExternalSource          (e.g., "RoofingJacket")
ExternalReferenceId     (e.g., jobId)
RequestedByUserId       (optional, great for audits)
```

### V1 Queue List (Minimal but Powerful)

| Queue | Purpose |
|-------|---------|
| `email-send` | All outbound email (invoice, dunning, generic) |
| `dunning-evaluate` | Daily dunning evaluation per tenant |
| `payment-commands` | Async payment processing + autopay charges |
| `webhook-ingest` | Inbound webhook normalization |

Dead-letter queues enabled on all. Four queues is enough to model real production workflows without creating microservice soup.

### What Runs Where

| Project | Role |
|---------|------|
| `BillerJacket.Web` (Razor Pages) | Human UI: dashboard, support tools |
| `BillerJacket.Api` (Web API) | Service boundary + integration endpoints |
| `BillerJacket.Worker` | Queue consumers + scheduled enqueuers |
| SQL | Truth |
| Service Bus | Reliability + retries + DLQ |
| Key Vault + Managed Identity | Secure config |
| App Insights / Log Analytics | "Explain what happened" |

---

## 4. Domain Model (Money-First)

### Core Entities

- `Tenant`
- `Customer`
- `Invoice`
- `InvoiceLineItem`
- `Payment`
- `PaymentAttempt`
- `DunningPlan`
- `DunningStep`
- `InvoiceDunningState`
- `CommunicationLog`
- `WebhookEvent`
- `IdempotencyKey`
- `AuditLog`
- `LedgerEntry` (optional)
- `BlogPost`
- `LandingPage`

### Required Fields Everywhere

```
TenantId
ExternalSource
ExternalReferenceId
```

This enables clean integration with RoofingJacket.

### ERD (Conceptual)

---

#### Tenant

| Column | Type |
|--------|------|
| `TenantId` | PK |
| `Name` | string |
| `DefaultCurrency` | string |
| `CreatedAt` | DateTimeOffset |

**Relationships:**

- Tenant 1 -> N Users
- Tenant 1 -> N Customers
- Tenant 1 -> N Invoices
- Tenant 1 -> N Payments
- Tenant 1 -> N DunningPlans
- Tenant 1 -> N WebhookEvents
- Tenant 1 -> N AuditLogs

---

#### User

| Column | Type |
|--------|------|
| `UserId` | PK |
| `TenantId` | FK -> Tenant |
| `Email` | string |
| `Role` | string |
| `CreatedAt` | DateTimeOffset |

---

#### Customer

| Column | Type |
|--------|------|
| `CustomerId` | PK |
| `TenantId` | FK -> Tenant |
| `DisplayName` | string |
| `Email` | string |
| `Phone` | string |
| `ExternalSource` | string |
| `ExternalReferenceId` | string |
| `IsActive` | bool |

**Relationships:**

- Customer 1 -> N Invoices
- Customer 1 -> N CommunicationLogs

---

#### Invoice

| Column | Type |
|--------|------|
| `InvoiceId` | PK |
| `TenantId` | FK -> Tenant |
| `CustomerId` | FK -> Customer |
| `InvoiceNumber` | string (unique per Tenant) |
| `Status` | string |
| `IssueDate` | DateOnly |
| `DueDate` | DateOnly |
| `CurrencyCode` | string |
| `SubtotalAmount` | decimal |
| `TaxAmount` | decimal |
| `TotalAmount` | decimal |
| `PaidAmount` | decimal |
| `BalanceDue` | decimal (computed) |
| `SentAt` | DateTimeOffset? |
| `PaidAt` | DateTimeOffset? |
| `ExternalSource` | string |
| `ExternalReferenceId` | string |

**Relationships:**

- Invoice 1 -> N InvoiceLineItems
- Invoice 1 -> N Payments
- Invoice 1 -> N PaymentAttempts
- Invoice 1 -> N CommunicationLogs
- Invoice 1 -> 1 InvoiceDunningState

---

#### InvoiceLineItem

| Column | Type |
|--------|------|
| `InvoiceLineItemId` | PK |
| `InvoiceId` | FK -> Invoice |
| `TenantId` | FK -> Tenant |
| `LineNumber` | int |
| `Description` | string |
| `Quantity` | decimal |
| `UnitPrice` | decimal |
| `LineTotal` | decimal (computed) |

---

#### Payment

| Column | Type |
|--------|------|
| `PaymentId` | PK |
| `TenantId` | FK -> Tenant |
| `InvoiceId` | FK -> Invoice |
| `Amount` | decimal |
| `CurrencyCode` | string |
| `Method` | string (Manual / Autopay / External) |
| `Status` | string (Succeeded / Pending / Failed / Reversed) |
| `AppliedAt` | DateTimeOffset |
| `ExternalProvider` | string |
| `ExternalPaymentId` | string |
| `CreatedByUserId` | FK -> User (optional) |
| `CorrelationId` | string |

**Relationships:**

- Payment N -> 1 Invoice
- Payment N -> 1 User (optional)

---

#### PaymentAttempt

| Column | Type |
|--------|------|
| `PaymentAttemptId` | PK |
| `TenantId` | FK -> Tenant |
| `InvoiceId` | FK -> Invoice |
| `Amount` | decimal |
| `CurrencyCode` | string |
| `Status` | string (Succeeded / Failed) |
| `FailureCode` | string |
| `FailureMessage` | string |
| `Provider` | string |
| `AttemptedAt` | DateTimeOffset |
| `CorrelationId` | string |

---

#### IdempotencyKey

| Column | Type |
|--------|------|
| `IdempotencyKeyId` | PK |
| `TenantId` | FK -> Tenant |
| `Operation` | string |
| `KeyValue` | string |
| `RequestHash` | string |
| `ResponseJson` | string |
| `CreatedAt` | DateTimeOffset |

Logical relationship to Payment via `Operation` + `CorrelationId`.

---

#### DunningPlan

| Column | Type |
|--------|------|
| `DunningPlanId` | PK |
| `TenantId` | FK -> Tenant |
| `Name` | string |
| `IsDefault` | bool |
| `IsActive` | bool |

**Relationships:**

- DunningPlan 1 -> N DunningSteps
- DunningPlan 1 -> N InvoiceDunningStates

---

#### DunningStep

| Column | Type |
|--------|------|
| `DunningStepId` | PK |
| `TenantId` | FK -> Tenant |
| `DunningPlanId` | FK -> DunningPlan |
| `StepNumber` | int |
| `DaysAfterDue` | int |
| `TemplateKey` | string |

---

#### InvoiceDunningState

| Column | Type |
|--------|------|
| `InvoiceId` | PK, FK -> Invoice |
| `TenantId` | FK -> Tenant |
| `DunningPlanId` | FK -> DunningPlan |
| `CurrentStepNumber` | int |
| `NextActionAt` | DateTimeOffset |
| `LastActionAt` | DateTimeOffset |

---

#### CommunicationLog

| Column | Type |
|--------|------|
| `CommunicationLogId` | PK |
| `TenantId` | FK -> Tenant |
| `Channel` | string (Email / SMS) |
| `Type` | string (Invoice / Dunning / Receipt) |
| `Status` | string (Sent / Failed) |
| `CustomerId` | FK -> Customer |
| `InvoiceId` | FK -> Invoice |
| `ToAddress` | string |
| `Subject` | string |
| `Provider` | string |
| `ProviderMessageId` | string |
| `ErrorMessage` | string |
| `SentAt` | DateTimeOffset |
| `CorrelationId` | string |

---

#### WebhookEvent

| Column | Type |
|--------|------|
| `WebhookEventId` | PK |
| `TenantId` | FK -> Tenant |
| `Provider` | string |
| `ExternalEventId` | string (unique per Tenant+Provider) |
| `EventType` | string |
| `PayloadJson` | string |
| `ProcessingStatus` | string |
| `ReceivedAt` | DateTimeOffset |
| `ProcessedAt` | DateTimeOffset |
| `ErrorMessage` | string |
| `CorrelationId` | string |

---

#### AuditLog

| Column | Type |
|--------|------|
| `AuditLogId` | PK |
| `TenantId` | FK -> Tenant |
| `EntityType` | string |
| `EntityId` | string |
| `Action` | string |
| `DataJson` | string |
| `PerformedByUserId` | FK -> User |
| `OccurredAt` | DateTimeOffset |
| `CorrelationId` | string |

---

#### BlogPost

> Global entity -- not tenant-scoped. SuperAdmin-only access.

| Column | Type |
|--------|------|
| `BlogPostId` | PK |
| `Title` | string |
| `Slug` | string (unique) |
| `Content` | string (markdown/HTML) |
| `Excerpt` | string |
| `Status` | string (Draft / Published) |
| `AuthorUserId` | FK -> User |
| `PublishedAt` | DateTimeOffset? |
| `CreatedAt` | DateTimeOffset |
| `UpdatedAt` | DateTimeOffset |

---

#### LandingPage

> Global entity -- not tenant-scoped. SuperAdmin-only access.

| Column | Type |
|--------|------|
| `LandingPageId` | PK |
| `Title` | string |
| `Slug` | string (unique) |
| `PageType` | string (Tool / Feature / CaseStudy) |
| `Content` | string (markdown/HTML) |
| `MetaDescription` | string |
| `Status` | string (Draft / Published) |
| `CreatedAt` | DateTimeOffset |
| `UpdatedAt` | DateTimeOffset |

---

### Relationship Tree

```
Tenant
 ├── Users
 ├── Customers
 │    └── Invoices
 │         ├── InvoiceLineItems
 │         ├── Payments
 │         ├── PaymentAttempts
 │         ├── CommunicationLogs
 │         └── InvoiceDunningState
 │              └── DunningPlan
 │                   └── DunningSteps
 ├── IdempotencyKeys
 ├── WebhookEvents
 └── AuditLogs

BlogPosts (global -- not tenant-scoped)
LandingPages (global -- not tenant-scoped)
```

### Why This ERD Works

- Clear bounded contexts (billing, payments, dunning, ops)
- Money flows are traceable
- Multi-tenant safe
- Supports async workflows cleanly
- Easy to explain in an interview
- Easy to evolve without schema rewrites

---

## 5. Razor Pages UI (Admin / Visibility)

**Audience:** business operators, finance, support
**Goal:** visibility and control, not polish

### Pages

| Route | Purpose |
|-------|---------|
| `/login` | Authentication |
| `/` | Dashboard |
| `/customers` | Customer list |
| `/invoices` | Invoice list |
| `/invoices/{id}` | Invoice detail |
| `/payments` | Payment list |
| `/activity` | Activity log |
| `/support/webhooks` | Webhook inspector |
| `/support/dlq` | Dead-letter queue viewer |
| `/admin` | SuperAdmin dashboard |
| `/admin/blog` | Blog post list (drafts + published) |
| `/admin/blog/edit/{id?}` | Blog post editor (create/edit) |
| `/admin/pages` | Landing page list |
| `/admin/pages/edit/{id?}` | Landing page editor |
| `/admin/tenants` | Tenant list (all orgs) |
| `/admin/tenants/edit/{id}` | Tenant editor (name, logo, settings) |

The UI is intentionally boring and explainable.

---

## 6. Web API (Service Boundary)

### Inbound Commands (from RoofingJacket)

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/invoices` | Create invoice |
| `POST` | `/api/invoices/{id}/send` | Send invoice |
| `POST` | `/api/payments` | Record payment (idempotent) |
| `POST` | `/api/dunning/run` | Trigger dunning evaluation |

### Webhooks

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/webhooks/{provider}` | Ingest provider webhook |
| `POST` | `/api/webhooks/{id}/replay` | Replay a webhook event |

### Design Rules

- Idempotency required for payment commands
- No UI assumptions
- Versioned routes (`/api/v1/...`)

---

## 7. Background Processing (Worker)

### Responsibilities

- Process Service Bus queues
- Execute long-running workflows
- Retry safely
- Write audit records

### Jobs

| Job | Description |
|-----|-------------|
| Daily dunning evaluation | Evaluate overdue invoices against dunning plans |
| Autopay execution | Stub for future automatic payment collection |
| Email send processing | Dequeue and send transactional emails |
| Webhook normalization and replay | Normalize inbound webhooks, support replay |

---

## 8. Security Defaults (Fintech-Grade)

- Decimal-only money (`decimal(18,2)` or `decimal(19,4)`)
- Idempotency keys enforced
- Immutable audit logs
- Managed Identity everywhere
- Least-privilege RBAC
- Correlation IDs across API -> queue -> worker

---

## 9. SDLC & Documentation (Portfolio-Critical)

### Documentation

```
docs/
├─ product-model.md        # What BillerJacket is
├─ workflows.md            # Invoice -> Payment -> Dunning
├─ database.md             # Schema + indexes
├─ support-playbook.md     # Replay, DLQ, tracing
└─ architecture.md         # Azure layout + boundaries
```

This is what makes interviewers relax.

### CI/CD (GitHub Actions)

**Pipeline:** `.github/workflows/`

#### `ci.yml` -- Build + Test (every push / PR)

1. Checkout
2. Setup .NET 10
3. `dotnet restore`
4. `dotnet build --no-restore`
5. `dotnet test --no-build` (unit + integration)
6. Fail the PR if any step fails

#### `deploy.yml` -- Deploy to Azure (on merge to `main`)

1. Run full CI steps
2. `dotnet publish` for each deployable project:
   - `BillerJacket.Web`
   - `BillerJacket.Api`
   - `BillerJacket.Worker`
3. Deploy to Azure App Service (one slot per project)
4. Run EF Core migrations against Azure SQL

#### Deployment Strategy

| Environment | Trigger | Purpose |
|-------------|---------|---------|
| Dev / Staging | Push to `main` | Validate in Azure |
| Production | Manual approval or tag | Ship when ready |

#### Secrets (stored in GitHub Secrets, injected at deploy)

- Azure publish profile or service principal credentials
- Azure SQL connection string (or use Managed Identity)
- Service Bus connection string

#### EF Core Migrations

- Migrations checked into source control
- Applied during deploy via `dotnet ef database update` or a migration bundle
- Never applied manually in production

---

## 10. RoofingJacket Integration (Later)

### RoofingJacket sends commands

- `CreateInvoice`
- `SendInvoice`

### BillerJacket responds with queries

- `BillingSummary`
- `InvoiceStatus`
- `PaymentHistory`

RoofingJacket:

- Displays billing state
- **Never mutates billing data**

---

## 11. Why This Is the Right Shape

- Clean separation of concerns
- Reusable across verticals
- Safe money handling
- Easy to explain
- Easy to operate
- Easy to hire for

This is real product architecture, not portfolio theater.

---

## 12. Feature List

### Product Scope

BillerJacket is an internal Billing & Accounts Receivable (AR) service, not a generic payment processor and not a loan system.

> Track money owed, collect it reliably, and explain what happened when something goes wrong.

---

### Core Platform Features (Foundational)

**Multi-Tenant Platform**

- Tenant (business) accounts
- Tenant-scoped data isolation
- Tenant settings (currency, terms defaults)
- Support for multiple products consuming the service (RoofingJacket, etc.)

**User Management (Internal)**

- Login / logout
- Tenant-scoped users
- Roles: SuperAdmin (platform), Admin, Finance, Support
- Audit who performed actions

---

### Customer Management

- Create / update / deactivate customers
- Customer contact info (email, phone)
- Billing preferences
- Default payment terms
- External reference linking (e.g. RoofingJacket job ID)
- Customer activity history (invoices, payments, comms)

---

### Invoice Management

**Invoice Creation**

- One-time invoices
- Installment invoices
- Recurring invoices (monthly, quarterly -- simple v1)
- Line items with descriptions
- Tax placeholder (non-calculated v1)
- Attach external references (job, contract, etc.)

**Invoice Lifecycle**

| Status | Description |
|--------|-------------|
| Draft | Created, not yet sent |
| Sent | Delivered to customer |
| Overdue | Past due date |
| Paid | Fully paid |
| Void / Cancelled | Cancelled or voided |

**Invoice Delivery**

- Send invoice (email with secure link placeholder)
- Resend invoice
- Track delivery attempts

**Invoice Visibility**

- Invoice list (filter by status, date, customer)
- Invoice detail view (timeline)
- Balance due calculation
- Payment history per invoice

---

### Payment Management

**Manual Payments**

- Apply payment to invoice
- Partial payments
- Overpayments (credit handling placeholder)
- Notes on payments

**Idempotent Payments (Fintech-grade)**

- `Idempotency-Key` required on payment APIs
- Safe retry without double charging
- Stored request/response snapshots

**Payment Attempts**

- Track each attempt (success/failure)
- Failure reason capture
- Timestamped audit trail

**Payment Status**

| Status | Description |
|--------|-------------|
| Pending | Awaiting processing |
| Succeeded | Payment confirmed |
| Failed | Payment failed |
| Reversed | Placeholder for future reversal support |

---

### Autopay (Foundational, Expandable)

- Autopay enrollment per customer or invoice
- Autopay scheduling rules (on due date, immediately)
- Autopay execution (stubbed gateway)
- Autopay retry logic (future)
- Autopay failure handling -> dunning

Real processors not needed for v1 -- structure matters.

---

### Dunning & Collections Automation

**Dunning Plans**

- Configurable per tenant
- Ordered steps (Day 0, Day 3, Day 7, etc.)
- Action types (email reminder, final notice)

**Dunning Execution**

- Daily evaluation job
- Detect overdue invoices
- Enqueue reminder actions
- Avoid duplicate reminders

**Dunning Visibility**

- Show next scheduled reminder
- Show reminder history
- Status per invoice

---

### Communication Logging

- Email/SMS attempt tracking (stub ok)
- Delivery status
- Failure reasons
- Timestamped communication history
- Linked to invoice and customer

This is huge for support and trust.

---

### Webhook & Integration Handling

**Webhook Ingestion**

- Receive external events (payment gateways, future systems)
- Store raw payloads
- Normalize events internally

**Webhook Replay**

- Replay failed or historical events
- Support tooling for debugging
- Safe idempotent processing

---

### Auditability & Money Safety

Immutable audit logs for:

- Invoice state changes
- Payments
- Retries
- Dunning actions

Additional:

- Optional ledger entries (debit/credit trail)
- Correlation IDs across API -> worker -> DB
- Full "what happened?" timeline

---

### Support & Operations Tooling

- Activity log dashboard
- Webhook replay UI
- Dead-letter queue visibility
- Manual reprocessing controls
- Trace invoice -> payment -> comms chain
- Notes for support staff

This is what real production teams need.

---

### SuperAdmin Administration

- Platform-wide dashboard (all tenants)
- Tenant list with search/filter
- Edit tenant (name, logo, primary color, enable/disable)
- Cross-tenant visibility (not scoped to one org)
- Manual SuperAdmin provisioning (DB seed or manual update)

---

### Blog Management (SuperAdmin)

- Create / edit / delete blog posts
- Draft → Published workflow
- Markdown/HTML content editor
- SEO: slug, excerpt, meta
- Filter by status (draft, published)
- Global (not tenant-scoped)

---

### Landing Pages CMS (SuperAdmin)

- Create / edit / delete landing pages
- Page types: Tool, Feature, CaseStudy
- Draft → Published workflow
- SEO metadata (title, description, slug)
- Global (not tenant-scoped)

---

### Dashboard & Reporting

**Business Dashboard**

- Total outstanding balance
- Overdue amount
- Paid this month
- Upcoming reminders

**Lists & Filters**

- Customers by balance
- Invoices by status
- Failed payments
- Recent activity

**Reporting (Read-heavy -- Dapper)**

- Aging report (30/60/90 days)
- Customer balances
- Payment success rates

---

### Security & Compliance Foundations

- Decimal-only money handling
- Transactions around money writes
- Idempotency enforcement
- Managed Identity (Azure)
- Secrets in Key Vault
- Least-privilege access
- Tenant isolation enforced everywhere

---

### Platform & SDLC Features

- EF Core migrations in source control
- Purposeful SQL indexes
- Background workers via Service Bus
- Retry + DLQ support
- Observability (App Insights)
- Structured logging
- Health checks

---

### Explicitly Out of Scope (By Design)

- Lending / interest calculation
- Credit underwriting
- Consumer wallet
- Payment processor replacement
- Accounting general ledger (full GAAP)

You are not building Stripe or QuickBooks -- and that's good.

---

### Feature Phasing

**V1 (Portfolio + Real Value)**

- Customers
- Invoices (one-time + installments)
- Manual payments (idempotent)
- Dunning (email reminders)
- Activity log
- Dashboard
- SuperAdmin administration (tenants, blog, pages)

**V1.5**

- Autopay
- Webhook replay UI
- Aging reports

**V2 (If it earns it)**

- Multiple currencies
- Accounting system sync
- Advanced dunning strategies
- Payment provider integrations

---

### Why This Feature Set Is Strong

- Realistic
- Cohesive
- Safe
- Shows judgment
- Maps directly to fintech SaaS roles
- Cleanly integrates into RoofingJacket

Every feature exists because a real business would need it.

---

## 13. Feature-to-Azure Service Mapping

Detailed mapping of each feature area to Azure services, queues, message types, and publisher/consumer responsibilities.

---

### Tenancy + Auth + Roles

**Azure Services:** App Service (Web/API), SQL Database (Tenants, Users, Roles), Key Vault (signing keys, connection strings)

**Queues:** None required for MVP

Keep auth synchronous. Log all auth events to App Insights.

---

### Customer Management

**Azure Services:** App Service (Web + API), SQL Database

**Queues (optional):** `email-send` for welcome emails or sync flows

| Publisher | Consumer | Message |
|-----------|----------|---------|
| Web / API | Worker | `EmailSendRequested` |

---

### Invoice Creation + Lifecycle

**Azure Services:** App Service (Web + API), SQL Database, App Insights (traceability)

**Queues:**

| Trigger | Queue | Message |
|---------|-------|---------|
| Invoice sent | `email-send` | `InvoiceEmailRequested` |
| State change | `events` (optional) | `InvoiceStatusChanged` |

| Publisher | Consumer |
|-----------|----------|
| API / UI publishes "send invoice" | Worker consumes email queue, writes `CommunicationLog` |

---

### Email / Notification Delivery + Communication Log

**Azure Services:** Service Bus, App Service Worker, SQL Database (`CommunicationLog`), App Insights / Log Analytics

**Queue:** `email-send`

| Message | Purpose |
|---------|---------|
| `InvoiceEmailRequested` | Send invoice email |
| `DunningEmailRequested` | Send dunning reminder |
| `EmailSendRequested` | Generic email send |

| Publisher | Consumer |
|-----------|----------|
| API (send / resend actions) | Worker consumes, sends (or stubs), stores outcome |

**DLQ Policy:** Poison messages (malformed email, template failure) go to DLQ. Support UI can replay.

---

### Payments + Idempotency

**Azure Services:** App Service API, SQL Database (Payments, PaymentAttempts, IdempotencyKeys, optional Ledger), Key Vault (gateway keys later), App Insights (trace idempotency + retries)

**Queues:** If manual payment is synchronous: none required. For async processor-like behavior:

| Queue | Message |
|-------|---------|
| `payment-commands` | `ApplyPaymentCommand` |

| Publisher | Consumer |
|-----------|----------|
| API publishes command (optional async) | Worker writes PaymentAttempt/Payment, updates invoice status |

**DLQ Policy:** Repeated failures (gateway down) -> DLQ. Support replay.

---

### Autopay Enrollment + Scheduled Charges

**Azure Services:** SQL Database (`AutopayEnrollments`), Service Bus, Worker, (optional) Azure Functions timer

**Queue:** `payment-commands`

| Message | Purpose |
|---------|---------|
| `ScheduleAutopayCharges` | Fan-out trigger |
| `ChargeInvoiceAutopayCommand` | Charge a specific invoice |

| Publisher | Consumer |
|-----------|----------|
| Scheduler publishes `ScheduleAutopayCharges` | Worker evaluates due invoices, enqueues `ChargeInvoiceAutopayCommand` |
| (from above) | Worker processes charge, writes `PaymentAttempt` |

---

### Dunning (Collections Automation)

**Azure Services:** SQL Database (DunningPlan, Steps, NextActionAt), Service Bus, Worker, (optional) Azure Functions timer

**Queues:** `dunning-evaluate`, `email-send`

| Message | Purpose |
|---------|---------|
| `EvaluateDunningCommand` | Daily evaluation per tenant |
| `DunningEmailRequested` | Send dunning reminder email |

| Publisher | Consumer |
|-----------|----------|
| Scheduler publishes `EvaluateDunningCommand` | Worker finds candidates, enqueues `DunningEmailRequested` |
| (from above) | Email worker consumes, logs comms |

**DLQ Policy:** Dunning evaluation failures -> DLQ (rare). Email send failures -> DLQ (common, supportable).

---

### Webhooks (Ingest + Normalize + Replay)

**Azure Services:** App Service API, SQL Database (`WebhookEvent` raw + normalized), Service Bus, Worker, App Insights

**Queue:** `webhook-ingest`

| Message | Purpose |
|---------|---------|
| `WebhookReceived` | Contains `WebhookEventId` pointer |
| `WebhookReplayRequested` | Replay a historical event |

| Publisher | Consumer |
|-----------|----------|
| API writes `WebhookEvent` row first (source of truth), then publishes `WebhookReceived` with DB id | Worker loads payload, normalizes, applies actions idempotently |
| Support UI triggers replay | Worker processes `WebhookReplayRequested` |

---

### Support Tooling (DLQ View, Replay, Trace)

**Azure Services:** Service Bus DLQ, SQL Database (audit + communication + attempts), App Insights + Log Analytics (correlation queries)

**Queues:** No new queues. Support actions publish back to existing queues:

| Action | Target Queue |
|--------|--------------|
| Replay webhook | `webhook-ingest` |
| Replay email | `email-send` |
| Replay payment command | `payment-commands` |

**Key Support Features:**

- Replay DLQ message
- Reprocess webhook by id
- Trace by `CorrelationId`
- Show full invoice timeline

---

### Dashboard + Reporting

**Azure Services:** SQL Database (read models / reporting queries via Dapper), App Service Web (Razor Pages), App Insights (slow query diagnostics)

**Queues:** None required for MVP

**Optional later:** `reporting-refresh` with `RefreshDashboardAggregates` for precomputed aggregates.

---

## 14. Messaging Implementation Guide

Reference implementation for Service Bus messaging across the BillerJacket solution. Covers contracts, publishing, consuming, correlation propagation, and retry/DLQ behavior.

**Stack:** .NET 10, `Azure.Messaging.ServiceBus`

---

### 14.1 Message Contracts (`BillerJacket.Contracts`)

All message contracts live in the shared `Contracts` project so both API (publisher) and Worker (consumer) reference the same types.

#### Base Interface & Envelope

`src/BillerJacket.Contracts/Messaging/MessageEnvelope.cs`

```csharp
namespace BillerJacket.Contracts.Messaging;

public interface IMessage
{
    string MessageType { get; }
    string TenantId { get; }
    string CorrelationId { get; }
    string? ExternalSource { get; }
    string? ExternalReferenceId { get; }
    string? RequestedByUserId { get; }
    DateTimeOffset OccurredAt { get; }
}

public abstract record MessageBase(
    string TenantId,
    string CorrelationId,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : IMessage
{
    public abstract string MessageType { get; }
}

/// <summary>
/// Envelope so consumers can always deserialize safely
/// without needing to know the concrete type up front.
/// </summary>
public sealed record MessageEnvelope(
    string MessageType,
    string PayloadJson,
    string TenantId,
    string CorrelationId,
    DateTimeOffset EnqueuedAt
);
```

#### Queue Name Constants

`src/BillerJacket.Contracts/Messaging/Queues.cs`

```csharp
namespace BillerJacket.Contracts.Messaging;

public static class Queues
{
    public const string EmailSend        = "email-send";
    public const string DunningEvaluate  = "dunning-evaluate";
    public const string PaymentCommands  = "payment-commands";
    public const string WebhookIngest    = "webhook-ingest";
}
```

#### Email Messages

`src/BillerJacket.Contracts/Messaging/EmailMessages.cs`

```csharp
namespace BillerJacket.Contracts.Messaging;

public sealed record InvoiceEmailRequested(
    string TenantId,
    string CorrelationId,
    string InvoiceId,
    string ToEmail,
    string Subject,
    string Body,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "email.invoice_requested";
}

public sealed record DunningEmailRequested(
    string TenantId,
    string CorrelationId,
    string InvoiceId,
    string CustomerId,
    int DunningStepNumber,
    string ToEmail,
    string Subject,
    string Body,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "email.dunning_requested";
}
```

#### Dunning Messages

`src/BillerJacket.Contracts/Messaging/DunningMessages.cs`

```csharp
namespace BillerJacket.Contracts.Messaging;

public sealed record EvaluateDunningCommand(
    string TenantId,
    string CorrelationId,
    DateOnly AsOfDate,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "dunning.evaluate";
}
```

#### Payment Messages

`src/BillerJacket.Contracts/Messaging/PaymentMessages.cs`

```csharp
namespace BillerJacket.Contracts.Messaging;

public sealed record ApplyPaymentCommand(
    string TenantId,
    string CorrelationId,
    string InvoiceId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "payment.apply";
}
```

#### Webhook Messages

`src/BillerJacket.Contracts/Messaging/WebhookMessages.cs`

```csharp
namespace BillerJacket.Contracts.Messaging;

public sealed record WebhookReceived(
    string TenantId,
    string CorrelationId,
    string Provider,
    string WebhookEventId,  // points to row in SQL (payload stored first)
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId, ExternalSource,
    ExternalReferenceId, RequestedByUserId, OccurredAt)
{
    public override string MessageType => "webhook.received";
}

public sealed record WebhookReplayRequested(
    string TenantId,
    string CorrelationId,
    string WebhookEventId,
    string? RequestedByUserId,
    DateTimeOffset OccurredAt
) : MessageBase(TenantId, CorrelationId,
    ExternalSource: null, ExternalReferenceId: null,
    RequestedByUserId, OccurredAt)
{
    public override string MessageType => "webhook.replay_requested";
}
```

#### Shared JSON Serializer

`src/BillerJacket.Contracts/Messaging/JsonDefaults.cs`

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BillerJacket.Contracts.Messaging;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
```

---

### 14.2 Publisher (`BillerJacket.Api`)

A safe Service Bus sender wrapper used by API controllers to publish messages with correlation and diagnostics.

`src/BillerJacket.Api/Infrastructure/Messaging/BusPublisher.cs`

```csharp
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BillerJacket.Contracts.Messaging;

namespace BillerJacket.Api.Infrastructure.Messaging;

public interface IBusPublisher
{
    Task PublishAsync(string queueName, IMessage message,
        CancellationToken ct = default);
}

public sealed class BusPublisher : IBusPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<BusPublisher> _logger;

    public BusPublisher(ServiceBusClient client,
        ILogger<BusPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync(string queueName, IMessage message,
        CancellationToken ct = default)
    {
        var sender = _client.CreateSender(queueName);

        var payloadJson = JsonSerializer.Serialize(
            message, message.GetType(), JsonDefaults.Options);

        var envelope = new MessageEnvelope(
            MessageType: message.MessageType,
            PayloadJson: payloadJson,
            TenantId: message.TenantId,
            CorrelationId: message.CorrelationId,
            EnqueuedAt: DateTimeOffset.UtcNow
        );

        var envelopeJson = JsonSerializer.Serialize(envelope, JsonDefaults.Options);

        var sbMessage = new ServiceBusMessage(
            Encoding.UTF8.GetBytes(envelopeJson))
        {
            ContentType = "application/json",
            CorrelationId = message.CorrelationId,
            Subject = message.MessageType,
            MessageId = Guid.NewGuid().ToString("n")
        };

        sbMessage.ApplicationProperties["tenantId"] = message.TenantId;
        sbMessage.ApplicationProperties["messageType"] = message.MessageType;

        if (!string.IsNullOrWhiteSpace(message.ExternalSource))
            sbMessage.ApplicationProperties["externalSource"] =
                message.ExternalSource!;
        if (!string.IsNullOrWhiteSpace(message.ExternalReferenceId))
            sbMessage.ApplicationProperties["externalReferenceId"] =
                message.ExternalReferenceId!;

        _logger.LogInformation(
            "Publishing {MessageType} to {Queue} " +
            "(tenant={TenantId}, corr={CorrelationId})",
            message.MessageType, queueName,
            message.TenantId, message.CorrelationId);

        await sender.SendMessageAsync(sbMessage, ct);
    }
}
```

**DI Registration** (`BillerJacket.Api/Program.cs`):

```csharp
builder.Services.AddSingleton(_ =>
{
    var cs = builder.Configuration.GetConnectionString("ServiceBus");
    return new ServiceBusClient(cs);
});
builder.Services.AddScoped<IBusPublisher, BusPublisher>();
```

In Azure: store Service Bus connection string in Key Vault and reference via App Service configuration. Later switch to Managed Identity with `DefaultAzureCredential`.

---

### 14.3 API Example: Payment Endpoint with Correlation + Idempotency

```csharp
using BillerJacket.Contracts.Messaging;
using BillerJacket.Api.Infrastructure.Messaging;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IBusPublisher _bus;

    public PaymentsController(IBusPublisher bus) => _bus = bus;

    [HttpPost]
    public async Task<IActionResult> ApplyPayment(
        [FromBody] ApplyPaymentRequest req, CancellationToken ct)
    {
        // Correlation: take incoming header or generate
        var correlationId =
            Request.Headers.TryGetValue("X-Correlation-Id", out var cid)
            && !string.IsNullOrWhiteSpace(cid)
                ? cid.ToString()
                : Guid.NewGuid().ToString("n");

        // Idempotency: required for payment APIs
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var ik)
            || string.IsNullOrWhiteSpace(ik))
            return BadRequest("Missing Idempotency-Key header.");

        var msg = new ApplyPaymentCommand(
            TenantId: req.TenantId,
            CorrelationId: correlationId,
            InvoiceId: req.InvoiceId,
            Amount: req.Amount,
            Currency: req.Currency ?? "USD",
            IdempotencyKey: ik!,
            ExternalSource: req.ExternalSource,
            ExternalReferenceId: req.ExternalReferenceId,
            RequestedByUserId: req.RequestedByUserId,
            OccurredAt: DateTimeOffset.UtcNow
        );

        await _bus.PublishAsync(Queues.PaymentCommands, msg, ct);
        return Accepted(new { correlationId });
    }
}

public sealed record ApplyPaymentRequest(
    string TenantId,
    string InvoiceId,
    decimal Amount,
    string? Currency,
    string? ExternalSource,
    string? ExternalReferenceId,
    string? RequestedByUserId
);
```

---

### 14.4 Worker: Queue Consumer with Routing + DLQ

`src/BillerJacket.Worker/Messaging/QueueProcessor.cs`

```csharp
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BillerJacket.Contracts.Messaging;

namespace BillerJacket.Worker.Messaging;

public sealed class QueueProcessor : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<QueueProcessor> _logger;
    private ServiceBusProcessor? _processor;

    public QueueProcessor(ServiceBusClient client,
        ILogger<QueueProcessor> logger)
    {
        _client = client;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = _client.CreateProcessor(
            Queues.PaymentCommands,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 4,
                PrefetchCount = 10
            });

        _processor.ProcessMessageAsync += OnMessageAsync;
        _processor.ProcessErrorAsync += OnErrorAsync;

        return _processor.StartProcessingAsync(stoppingToken);
    }

    private async Task OnMessageAsync(ProcessMessageEventArgs args)
    {
        var body = Encoding.UTF8.GetString(args.Message.Body);

        MessageEnvelope envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<MessageEnvelope>(
                           body, JsonDefaults.Options)
                       ?? throw new InvalidOperationException(
                           "Envelope was null.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to deserialize envelope. " +
                "Dead-lettering messageId={MessageId}",
                args.Message.MessageId);
            await args.DeadLetterMessageAsync(
                args.Message, "bad_envelope", ex.Message);
            return;
        }

        using var scope = _logger.BeginScope(
            new Dictionary<string, object?>
            {
                ["tenantId"] = envelope.TenantId,
                ["correlationId"] = envelope.CorrelationId,
                ["messageType"] = envelope.MessageType,
                ["messageId"] = args.Message.MessageId
            });

        _logger.LogInformation(
            "Processing {MessageType} (delivery={DeliveryCount})",
            envelope.MessageType, args.Message.DeliveryCount);

        try
        {
            switch (envelope.MessageType)
            {
                case "payment.apply":
                {
                    var cmd = JsonSerializer.Deserialize<ApplyPaymentCommand>(
                        envelope.PayloadJson, JsonDefaults.Options)
                        ?? throw new InvalidOperationException(
                            "ApplyPaymentCommand null");
                    await HandleApplyPayment(cmd, args.CancellationToken);
                    break;
                }

                default:
                    _logger.LogWarning(
                        "Unknown messageType {MessageType}. Dead-lettering.",
                        envelope.MessageType);
                    await args.DeadLetterMessageAsync(
                        args.Message, "unknown_message_type",
                        envelope.MessageType);
                    return;
            }

            await args.CompleteMessageAsync(args.Message);
        }
        catch (TransientException tex)
        {
            // Let it retry (abandon).
            // After MaxDeliveryCount, Service Bus DLQs automatically.
            _logger.LogWarning(tex,
                "Transient failure. Abandoning for retry.");
            await args.AbandonMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            // Non-transient: DLQ immediately with reason.
            _logger.LogError(ex,
                "Non-transient failure. Dead-lettering.");
            await args.DeadLetterMessageAsync(
                args.Message, "processing_failed", ex.Message);
        }
    }

    private Task OnErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            "ServiceBus error source={ErrorSource}, " +
            "entity={EntityPath}, ns={Namespace}",
            args.ErrorSource, args.EntityPath,
            args.FullyQualifiedNamespace);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken ct)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(ct);
            await _processor.DisposeAsync();
        }
        await base.StopAsync(ct);
    }

    // ---- Handlers (stub) ----
    private Task HandleApplyPayment(
        ApplyPaymentCommand cmd, CancellationToken ct)
    {
        // 1) Check IdempotencyKey in SQL
        // 2) Create PaymentAttempt
        // 3) Apply payment transactionally
        // 4) Update Invoice status if fully paid
        // 5) Write audit/ledger entries
        _logger.LogInformation(
            "ApplyPayment invoice={InvoiceId} " +
            "amount={Amount} {Currency} idem={IdempotencyKey}",
            cmd.InvoiceId, cmd.Amount, cmd.Currency,
            cmd.IdempotencyKey);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Throw this to indicate "retry me" (transient failure).
/// </summary>
public sealed class TransientException : Exception
{
    public TransientException(string message, Exception? inner = null)
        : base(message, inner) { }
}
```

**DI Registration** (`BillerJacket.Worker/Program.cs`):

```csharp
builder.Services.AddSingleton(_ =>
{
    var cs = builder.Configuration.GetConnectionString("ServiceBus");
    return new ServiceBusClient(cs);
});
builder.Services.AddHostedService<QueueProcessor>();
```

---

### 14.5 Recommended Worker Structure (One Processor per Queue)

Instead of one giant switch, use one `BackgroundService` per queue:

```
Worker/
  Messaging/
    PaymentsProcessorHostedService.cs    # payment-commands
    DunningProcessorHostedService.cs     # dunning-evaluate
    WebhookProcessorHostedService.cs     # webhook-ingest
    EmailProcessorHostedService.cs       # email-send
```

This reads much better to employers than one mega-processor. Each processor owns its own message routing, error handling, and handler methods.

---

### 14.6 DLQ + Retry Behavior

#### Azure Service Bus Queue Settings

| Setting | Value | Rationale |
|---------|-------|-----------|
| Max delivery count | 10 | Typical for retryable workflows |
| Lock duration | 30s -- 1m | Depends on processing time |
| `AutoCompleteMessages` | `false` | You control success/failure explicitly |

#### In Code

| Scenario | Action |
|----------|--------|
| Transient exception (gateway timeout, SQL transient) | `AbandonMessageAsync` -- retries automatically |
| Invalid payload / bad envelope | `DeadLetterMessageAsync` immediately |
| Unknown message type | `DeadLetterMessageAsync` immediately |
| After `MaxDeliveryCount` reached | Service Bus DLQs automatically |

---

### 14.7 Correlation Propagation Checklist

| Layer | Responsibility |
|-------|----------------|
| **API** | Read `X-Correlation-Id` header or generate a new one |
| **Publisher** | Set `ServiceBusMessage.CorrelationId`, `ApplicationProperties["tenantId"]`, `Subject = messageType` |
| **Worker** | Start a logging scope with `tenantId` + `correlationId` from envelope |
| **DB writes** | Write `CorrelationId` into domain rows (`PaymentAttempt`, `WebhookEvent`, `CommunicationLog`) |

This gives you support-grade traceability: any support query can trace from API request through queue processing to database state.

---

### 14.8 Outbox Pattern (Optional, Future)

For scenarios where you need guaranteed "write to DB + publish to bus" atomicity:

1. Write the message to an `OutboxMessages` table in the same SQL transaction as your domain write
2. A background poller reads unpublished rows, publishes to Service Bus, marks as sent
3. Avoids the scenario where DB commits but bus publish fails (or vice versa)

Not needed for v1 -- the current "publish after DB write" approach is fine for a portfolio project and most production systems. Add the outbox if you hit real reliability requirements or want to demonstrate the pattern.

---

## 15. Authentication & Multi-Tenancy

### 15.1 Strategy Overview (Two Auth Surfaces)

| Surface | Mechanism | Consumer |
|---------|-----------|----------|
| Web UI (Razor Pages) | ASP.NET Identity + cookie auth | Human operators (finance, support, admin) |
| API (Web API) | API key in `X-Api-Key` header | RoofingJacket and future verticals |

Both surfaces resolve a `TenantId` for every request. All downstream queries filter by TenantId.

### 15.2 Identity Setup (Same Pattern as RoofingJacket)

- `AppUser` extends `IdentityUser` (minimal -- no extra properties)
- `AppIdentityDbContext` manages Identity tables only (via EF Core)
- Application user data lives in the `User` table (Section 4 ERD), loaded via Dapper
- Password policy: 8+ chars, upper + lower + digit, no special char required

Reference files from RoofingJacket:
- `Infrastructure/Identity/AppUser.cs`
- `Infrastructure/Identity/AppIdentityDbContext.cs`

### 15.3 Custom Claims Principal Factory

Bridges ASP.NET Identity to the `User` table. On sign-in, loads profile via Dapper and injects claims:

| Claim | Source | Purpose |
|-------|--------|---------|
| `user_id` | `User.UserId` | Application user identity |
| `tenant_id` | `User.TenantId` | Tenant scoping |
| `role` | `User.Role` | Authorization (SuperAdmin, Admin, Finance, Support) |
| `email` | `User.Email` | Display / audit |

Reference: RoofingJacket `CustomClaimsPrincipalFactory.cs`

### 15.4 Cookie Configuration

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/login";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});
```

### 15.5 Roles

| Role | Purpose |
|------|---------|
| SuperAdmin | Global platform admin -- blog, pages CMS, tenant management. Not tenant-scoped. |
| Admin | Full tenant access -- all features within their org |
| Finance | Invoice, payment, reporting access |
| Support | Read-only + replay/DLQ tooling |

Stored in `User.Role` column. Enforced via claim-based policies.

> SuperAdmin is a platform-level role (same pattern as RoofingJacket). SuperAdmin users do not require a TenantId and are excluded from setup redirect. Provisioned manually via DB seed -- no self-service creation.

### 15.6 Folder Authorization Conventions

```csharp
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");           // All pages require auth
    options.Conventions.AllowAnonymousToPage("/login"); // Except login
    options.Conventions.AuthorizeFolder("/Admin", "SuperAdmin"); // Platform admin
});
```

Policies:
```csharp
options.AddPolicy("AdminOnly", p => p.RequireClaim("role", "Admin"));
options.AddPolicy("SuperAdmin", p => p.RequireClaim("role", "SuperAdmin"));
```

### 15.7 Multi-Tenancy Resolution

**Web UI:** TenantId comes from claims (set at login by ClaimsPrincipalFactory).

**API:** TenantId comes from `X-Tenant-Id` header on every request. Validated against the API key's allowed tenants.

**EF Core:** Global query filter on all entities:
```csharp
modelBuilder.Entity<Invoice>().HasQueryFilter(e => e.TenantId == _tenantId);
```

**Dapper:** Every query includes `WHERE TenantId = @TenantId`.

> **SuperAdmin:** Not tenant-scoped. SuperAdmin users have no TenantId in claims. Blog posts and landing pages are global entities with no TenantId column. The `/admin/*` pages do not apply tenant filtering. For tenant management views, SuperAdmin queries across all tenants.

### 15.8 API Authentication (Service-to-Service)

**v1: API Key**

RoofingJacket passes:
- `X-Api-Key: <key>` -- stored in Key Vault, validated by middleware
- `X-Tenant-Id: <guid>` -- which tenant this request is for

API key middleware:
1. Read `X-Api-Key` header
2. Look up key in `ApiKeys` table (TenantId, KeyHash, IsActive, CreatedAt)
3. Validate key belongs to the claimed TenantId
4. Set TenantId in `HttpContext.Items` for downstream use
5. Reject with 401 if missing/invalid

**Later:** Upgrade to Entra ID app registration + JWT bearer tokens.

New entity for ERD:

| Column | Type |
|--------|------|
| `ApiKeyId` | PK |
| `TenantId` | FK -> Tenant |
| `KeyHash` | string (hashed, never stored plain) |
| `Name` | string (e.g. "RoofingJacket Prod") |
| `IsActive` | bool |
| `CreatedAt` | DateTimeOffset |

### 15.9 Setup Redirect Middleware

Same pattern as RoofingJacket `SetupRedirectMiddleware`:
- After first login, if user has no TenantId → redirect to `/setup`
- Setup page creates Tenant + assigns user as Admin
- Refresh sign-in to pick up new claims
- Excluded paths: `/login`, `/logout`, `/setup`, static assets

### 15.10 Context Accessor (`Current`)

Static class providing request-scoped access to identity context. Works in both HTTP requests and worker contexts.

```csharp
public static class Current
{
    public static Guid TenantId => ...;
    public static Guid? TenantIdOrNull => ...;
    public static Guid UserId => ...;
    public static Guid? UserIdOrNull => ...;
    public static string Role => ...;
    public static bool IsAuthenticated => ...;
    public static bool IsSuperAdmin => Role == "SuperAdmin";
    public static bool IsSystemUser => UserIdOrNull == SystemUser.Id;
}
```

Reference: RoofingJacket `Common/Current.cs`

### 15.11 SystemUser

Well-known identity for automated actions (dunning jobs, autopay, worker processing):

```csharp
public static class SystemUser
{
    public static readonly Guid Id = new("00000000-0000-0000-0000-000000575753");
    public const string Name = "System";
    public const string Email = "system@billerjacket.com";
}
```

Used in `AuditLog.PerformedByUserId` and `Payment.CreatedByUserId` for automated operations.

### 15.12 Worker Context Propagation

Service Bus messages already carry `TenantId` + `CorrelationId` in the message envelope (see Section 14.7). Workers set logging scope from envelope fields. No additional auth needed -- workers are internal and trusted.

### 15.13 OAuth (v1.5)

Deferred. When added:
- Google + Microsoft external login
- Same pattern as RoofingJacket `ExternalLogin.cshtml.cs`
- Email-based account linking
- Auto-create profile on first OAuth login

### 15.14 Claims Extensions

```csharp
public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal user) => ...;
    public static Guid? GetTenantId(this ClaimsPrincipal user) => ...;
    public static string? GetRole(this ClaimsPrincipal user) => ...;
    public static string? GetEmail(this ClaimsPrincipal user) => ...;
}
```

Reference: RoofingJacket `Common/Extensions/ClaimsPrincipalExtensions.cs`

---

## Final Framing

> "BillerJacket is a billing and AR service with a stable API. RoofingJacket consumes it but doesn't own money logic. That boundary keeps both systems simpler and safer."

That sentence alone puts you in senior-engineer territory.
