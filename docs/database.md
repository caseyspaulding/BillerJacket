# BillerJacket -- Database Schema

## Overview

- **Engine:** Azure SQL Database
- **ORM (Writes):** EF Core (code-first, migrations)
- **Query (Reads):** Dapper for reporting/dashboard queries
- **Money type:** `decimal(18,2)` -- no floats, no doubles
- **Timestamps:** `DateTimeOffset` for all temporal fields
- **Dates:** `DateOnly` for business dates (IssueDate, DueDate)
- **Multi-tenancy:** `TenantId` on all tenant-scoped entities, enforced via EF Core global query filters

---

## Entity Relationship Tree

```
Tenant
 +-- Users
 +-- Customers
 |    +-- Invoices
 |         +-- InvoiceLineItems
 |         +-- Payments
 |         +-- PaymentAttempts
 |         +-- CommunicationLogs
 |         +-- InvoiceDunningState
 |              +-- DunningPlan
 |                   +-- DunningSteps
 +-- ApiKeys
 +-- IdempotencyKeys
 +-- WebhookEvents
 +-- AuditLogs

BlogPosts (global -- not tenant-scoped)
LandingPages (global -- not tenant-scoped)
```

---

## Tables

### Tenant

| Column | Type | Notes |
|--------|------|-------|
| TenantId | uniqueidentifier PK | |
| Name | nvarchar | |
| DefaultCurrency | nvarchar | Default: "USD" |
| CreatedAt | datetimeoffset | |

Relationships: 1:N Users, Customers, Invoices, Payments, DunningPlans, WebhookEvents, AuditLogs

---

### User

| Column | Type | Notes |
|--------|------|-------|
| UserId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| Email | nvarchar | |
| Role | nvarchar | SuperAdmin, Admin, Finance, Support |
| CreatedAt | datetimeoffset | |

---

### Customer

| Column | Type | Notes |
|--------|------|-------|
| CustomerId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| DisplayName | nvarchar | |
| Email | nvarchar | |
| Phone | nvarchar | |
| ExternalSource | nvarchar | e.g. "RoofingJacket" |
| ExternalReferenceId | nvarchar | e.g. jobId |
| IsActive | bit | |

Relationships: 1:N Invoices, CommunicationLogs

---

### Invoice

| Column | Type | Notes |
|--------|------|-------|
| InvoiceId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| CustomerId | uniqueidentifier FK -> Customer | |
| InvoiceNumber | nvarchar | Unique per Tenant |
| Status | nvarchar | Draft, Sent, Overdue, Paid, Void, Cancelled |
| IssueDate | date | |
| DueDate | date | |
| CurrencyCode | nvarchar | |
| SubtotalAmount | decimal(18,2) | |
| TaxAmount | decimal(18,2) | |
| TotalAmount | decimal(18,2) | |
| PaidAmount | decimal(18,2) | |
| BalanceDue | decimal(18,2) | Computed: TotalAmount - PaidAmount |
| SentAt | datetimeoffset | Nullable |
| PaidAt | datetimeoffset | Nullable |
| ExternalSource | nvarchar | |
| ExternalReferenceId | nvarchar | |

Relationships: 1:N InvoiceLineItems, Payments, PaymentAttempts, CommunicationLogs; 1:1 InvoiceDunningState

---

### InvoiceLineItem

| Column | Type | Notes |
|--------|------|-------|
| InvoiceLineItemId | uniqueidentifier PK | |
| InvoiceId | uniqueidentifier FK -> Invoice | |
| TenantId | uniqueidentifier FK -> Tenant | |
| LineNumber | int | |
| Description | nvarchar | |
| Quantity | decimal(18,4) | |
| UnitPrice | decimal(18,2) | |
| LineTotal | decimal(18,2) | Computed: Quantity * UnitPrice |

---

### Payment

| Column | Type | Notes |
|--------|------|-------|
| PaymentId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| InvoiceId | uniqueidentifier FK -> Invoice | |
| Amount | decimal(18,2) | |
| CurrencyCode | nvarchar | |
| Method | nvarchar | Manual, Autopay, External |
| Status | nvarchar | Succeeded, Pending, Failed, Reversed |
| AppliedAt | datetimeoffset | |
| ExternalProvider | nvarchar | |
| ExternalPaymentId | nvarchar | |
| CreatedByUserId | uniqueidentifier FK -> User | Nullable |
| CorrelationId | nvarchar | |

---

### PaymentAttempt

| Column | Type | Notes |
|--------|------|-------|
| PaymentAttemptId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| InvoiceId | uniqueidentifier FK -> Invoice | |
| Amount | decimal(18,2) | |
| CurrencyCode | nvarchar | |
| Status | nvarchar | Succeeded, Failed |
| FailureCode | nvarchar | |
| FailureMessage | nvarchar | |
| Provider | nvarchar | |
| AttemptedAt | datetimeoffset | |
| CorrelationId | nvarchar | |

---

### IdempotencyKey

| Column | Type | Notes |
|--------|------|-------|
| IdempotencyKeyId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| Operation | nvarchar | |
| KeyValue | nvarchar | |
| RequestHash | nvarchar | |
| ResponseJson | nvarchar(max) | |
| CreatedAt | datetimeoffset | |

Logical relationship to Payment via Operation + CorrelationId.

---

### DunningPlan

| Column | Type | Notes |
|--------|------|-------|
| DunningPlanId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| Name | nvarchar | |
| IsDefault | bit | |
| IsActive | bit | |

Relationships: 1:N DunningSteps, InvoiceDunningStates

---

### DunningStep

| Column | Type | Notes |
|--------|------|-------|
| DunningStepId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| DunningPlanId | uniqueidentifier FK -> DunningPlan | |
| StepNumber | int | |
| DaysAfterDue | int | |
| TemplateKey | nvarchar | |

---

### InvoiceDunningState

| Column | Type | Notes |
|--------|------|-------|
| InvoiceId | uniqueidentifier PK, FK -> Invoice | |
| TenantId | uniqueidentifier FK -> Tenant | |
| DunningPlanId | uniqueidentifier FK -> DunningPlan | |
| CurrentStepNumber | int | |
| NextActionAt | datetimeoffset | |
| LastActionAt | datetimeoffset | |

---

### CommunicationLog

| Column | Type | Notes |
|--------|------|-------|
| CommunicationLogId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| Channel | nvarchar | Email, SMS |
| Type | nvarchar | Invoice, Dunning, Receipt |
| Status | nvarchar | Sent, Failed |
| CustomerId | uniqueidentifier FK -> Customer | |
| InvoiceId | uniqueidentifier FK -> Invoice | |
| ToAddress | nvarchar | |
| Subject | nvarchar | |
| Provider | nvarchar | |
| ProviderMessageId | nvarchar | |
| ErrorMessage | nvarchar | |
| SentAt | datetimeoffset | |
| CorrelationId | nvarchar | |

---

### WebhookEvent

| Column | Type | Notes |
|--------|------|-------|
| WebhookEventId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| Provider | nvarchar | |
| ExternalEventId | nvarchar | Unique per Tenant+Provider |
| EventType | nvarchar | |
| PayloadJson | nvarchar(max) | |
| ProcessingStatus | nvarchar | Received, Processing, Processed, Failed |
| ReceivedAt | datetimeoffset | |
| ProcessedAt | datetimeoffset | Nullable |
| ErrorMessage | nvarchar | |
| CorrelationId | nvarchar | |

---

### AuditLog

| Column | Type | Notes |
|--------|------|-------|
| AuditLogId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| EntityType | nvarchar | |
| EntityId | nvarchar | |
| Action | nvarchar | |
| DataJson | nvarchar(max) | |
| PerformedByUserId | uniqueidentifier FK -> User | |
| OccurredAt | datetimeoffset | |
| CorrelationId | nvarchar | |

---

### ApiKeyRecord

| Column | Type | Notes |
|--------|------|-------|
| ApiKeyId | uniqueidentifier PK | |
| TenantId | uniqueidentifier FK -> Tenant | |
| KeyHash | nvarchar | Hashed, never stored plain |
| Name | nvarchar | e.g. "RoofingJacket Prod" |
| IsActive | bit | |
| CreatedAt | datetimeoffset | |

---

### BlogPost (Global)

| Column | Type | Notes |
|--------|------|-------|
| BlogPostId | uniqueidentifier PK | |
| Title | nvarchar | |
| Slug | nvarchar | Unique |
| Content | nvarchar(max) | Markdown/HTML |
| Excerpt | nvarchar | |
| Status | nvarchar | Draft, Published |
| AuthorUserId | uniqueidentifier FK -> User | |
| PublishedAt | datetimeoffset | Nullable |
| CreatedAt | datetimeoffset | |
| UpdatedAt | datetimeoffset | |

---

### LandingPage (Global)

| Column | Type | Notes |
|--------|------|-------|
| LandingPageId | uniqueidentifier PK | |
| Title | nvarchar | |
| Slug | nvarchar | Unique |
| PageType | nvarchar | Tool, Feature, CaseStudy |
| Content | nvarchar(max) | Markdown/HTML |
| MetaDescription | nvarchar | |
| Status | nvarchar | Draft, Published |
| CreatedAt | datetimeoffset | |
| UpdatedAt | datetimeoffset | |

---

## Recommended Indexes

### Invoice Queries

| Index | Purpose |
|-------|---------|
| `IX_Invoice_TenantId_Status` on (TenantId, Status) | Filter invoices by status |
| `IX_Invoice_TenantId_CustomerId_DueDate` on (TenantId, CustomerId, DueDate) | Aging reports |
| `IX_Invoice_TenantId_DueDate` filtered WHERE Status IN ('Sent','Overdue') | Dunning evaluation |
| `IX_Invoice_TenantId_InvoiceNumber` on (TenantId, InvoiceNumber) UNIQUE | Invoice number lookup |

### Payment Queries

| Index | Purpose |
|-------|---------|
| `IX_Payment_TenantId_InvoiceId` on (TenantId, InvoiceId) | Payment lookup per invoice |
| `IX_Payment_TenantId_AppliedAt` on (TenantId, AppliedAt) | Recent payments dashboard |

### Idempotency

| Index | Purpose |
|-------|---------|
| `IX_IdempotencyKey_TenantId_Operation_KeyValue` on (TenantId, Operation, KeyValue) UNIQUE | Idempotency check |

### Dunning

| Index | Purpose |
|-------|---------|
| `IX_InvoiceDunningState_NextActionAt` on (TenantId, NextActionAt) | Daily dunning evaluation scan |

### Webhooks

| Index | Purpose |
|-------|---------|
| `IX_WebhookEvent_TenantId_Provider_ExternalEventId` UNIQUE | Deduplication |
| `IX_WebhookEvent_ProcessingStatus` on (TenantId, ProcessingStatus) | Failed webhook queries |

### Communication

| Index | Purpose |
|-------|---------|
| `IX_CommunicationLog_TenantId_InvoiceId` on (TenantId, InvoiceId) | Invoice communication history |

### Audit

| Index | Purpose |
|-------|---------|
| `IX_AuditLog_TenantId_EntityType_EntityId` on (TenantId, EntityType, EntityId) | Entity audit trail |
| `IX_AuditLog_CorrelationId` on (CorrelationId) | Trace by correlation |

### Content

| Index | Purpose |
|-------|---------|
| `IX_BlogPost_Slug` UNIQUE | URL routing |
| `IX_LandingPage_Slug` UNIQUE | URL routing |

---

## EF Core Configuration

Entity configurations live in `BillerJacket.Infrastructure/Data/Configurations/`. Each entity has a dedicated `IEntityTypeConfiguration<T>` class.

### Global Query Filter (Multi-Tenancy)

```csharp
modelBuilder.Entity<Invoice>().HasQueryFilter(e => e.TenantId == _tenantId);
```

Applied to all tenant-scoped entities. BlogPost and LandingPage are excluded (global entities).

### Dapper Queries

Read-heavy queries use Dapper in `BillerJacket.Infrastructure/Reporting/`. Every Dapper query includes `WHERE TenantId = @TenantId`.

```
Infrastructure/
  Reporting/
    InvoiceDashboardQueries.cs
    CustomerAgingQueries.cs
```

---

## Migration Strategy

- Migrations checked into source control
- Applied during deploy via `dotnet ef database update` or migration bundle
- Never applied manually in production
