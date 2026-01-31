# BillerJacket -- Workflows

## Invoice Lifecycle

### Status Flow

```
Draft --> Sent --> Overdue --> Paid
  |         |                   ^
  |         +----> Paid --------+
  |         |
  v         v
 Void    Cancelled
```

| Status | Description |
|--------|-------------|
| Draft | Created, not yet sent |
| Sent | Delivered to customer |
| Overdue | Past due date, not fully paid |
| Paid | Fully paid (PaidAmount >= TotalAmount) |
| Void | Cancelled before sending or by admin action |
| Cancelled | Cancelled after sending |

### Invoice Creation Flow

1. API receives `POST /api/invoices` (from RoofingJacket or Web UI)
2. Validate tenant, customer, line items
3. Calculate SubtotalAmount, TaxAmount, TotalAmount
4. Persist Invoice + InvoiceLineItems in a transaction
5. Write AuditLog entry
6. Return invoice with status `Draft`

### Invoice Send Flow

1. API receives `POST /api/invoices/{id}/send`
2. Validate invoice exists, status is `Draft`
3. Transition status to `Sent`, set `SentAt`
4. Publish `InvoiceEmailRequested` to `email-send` queue
5. Write AuditLog entry
6. Worker consumes message, sends email (or stubs), writes `CommunicationLog`

---

## Payment Flow

### Manual Payment Application

1. API receives `POST /api/payments` with `Idempotency-Key` header
2. Check idempotency: look up `IdempotencyKey(TenantId, Operation, KeyValue)`
   - If exists: return stored response (safe retry)
   - If new: proceed
3. Load invoice, validate it accepts payments (status is Sent or Overdue)
4. In a transaction:
   - Create `PaymentAttempt` (status: Succeeded)
   - Create `Payment` record
   - Update `Invoice.PaidAmount`
   - If `PaidAmount >= TotalAmount`: transition status to `Paid`, set `PaidAt`
   - Write `IdempotencyKey` with response
   - Write `AuditLog` entry
5. Return payment confirmation with CorrelationId

### Async Payment (via Service Bus)

1. API publishes `ApplyPaymentCommand` to `payment-commands` queue
2. Returns `202 Accepted` with CorrelationId
3. Worker consumes message:
   - Check idempotency
   - Create PaymentAttempt
   - Apply payment transactionally
   - Update invoice status if fully paid
   - Write audit/ledger entries
4. On transient failure: abandon message (Service Bus retries)
5. On non-transient failure: dead-letter immediately with reason

### Payment Failure Handling

| Scenario | Action |
|----------|--------|
| Transient failure (gateway timeout, SQL transient) | Abandon message, Service Bus retries |
| Invalid payload | Dead-letter immediately |
| Idempotency key collision with different request | Reject with conflict |
| After MaxDeliveryCount (10) | Service Bus auto-DLQs |

---

## Dunning Flow

### Configuration

Each tenant has one or more `DunningPlan`s, each with ordered `DunningStep`s:

```
DunningPlan: "Standard Collections"
  Step 1: Day 0   -> "Friendly Reminder" email
  Step 2: Day 3   -> "Payment Overdue" email
  Step 3: Day 7   -> "Final Notice" email
  Step 4: Day 14  -> "Collections Warning" email
```

### Daily Evaluation

1. Scheduler publishes `EvaluateDunningCommand` to `dunning-evaluate` queue (one per tenant)
2. Worker consumes:
   - Query all overdue invoices for the tenant
   - For each invoice with an `InvoiceDunningState`:
     - Check if `NextActionAt <= now`
     - If yes: publish `DunningEmailRequested` to `email-send` queue
     - Advance `CurrentStepNumber`, update `NextActionAt` and `LastActionAt`
   - For overdue invoices without dunning state:
     - Create `InvoiceDunningState` linked to tenant's default DunningPlan
     - Set `CurrentStepNumber = 1`, calculate `NextActionAt`

### Dunning Email Delivery

1. Worker consumes `DunningEmailRequested` from `email-send` queue
2. Resolve email template from `DunningStep.TemplateKey`
3. Send email (or stub)
4. Write `CommunicationLog` entry (channel: Email, type: Dunning)
5. On failure: write CommunicationLog with status Failed, message goes to DLQ after retries

### Dunning Termination

Dunning stops when:
- Invoice is paid (status transitions to `Paid`)
- Invoice is voided/cancelled
- All dunning steps have been executed
- Tenant deactivates the dunning plan

---

## Webhook Flow

### Ingest

1. API receives `POST /api/webhooks/{provider}`
2. Store raw payload in `WebhookEvent` row (status: `Received`)
3. Publish `WebhookReceived` to `webhook-ingest` queue (with `WebhookEventId` pointer)
4. Return `200 OK` immediately

### Processing

1. Worker consumes `WebhookReceived`
2. Load `WebhookEvent` row from SQL
3. Update status to `Processing`
4. Normalize event based on provider + event type
5. Apply actions idempotently (e.g., mark payment as confirmed)
6. Update status to `Processed`, set `ProcessedAt`
7. On failure: update status to `Failed`, set `ErrorMessage`

### Replay

1. Support UI triggers replay for a specific `WebhookEventId`
2. Publishes `WebhookReplayRequested` to `webhook-ingest` queue
3. Worker processes identically to original (idempotent)

---

## Email Delivery Flow

All outbound email flows through the `email-send` queue.

### Message Types

| Message | Source | Purpose |
|---------|--------|---------|
| `InvoiceEmailRequested` | Invoice send action | Deliver invoice to customer |
| `DunningEmailRequested` | Dunning evaluation | Send collection reminder |
| `EmailSendRequested` | Various | Generic email (welcome, etc.) |

### Processing

1. Worker consumes from `email-send` queue
2. Resolve recipient, subject, body from message
3. Send via email provider (stubbed in V1)
4. Write `CommunicationLog`:
   - Status: `Sent` or `Failed`
   - Provider + ProviderMessageId
   - ErrorMessage if failed
5. On failure: abandon for retry. After MaxDeliveryCount: DLQ.

---

## Correlation & Traceability

Every flow propagates a `CorrelationId`:

| Layer | Responsibility |
|-------|----------------|
| API | Read `X-Correlation-Id` header or generate new |
| Publisher | Set on `ServiceBusMessage.CorrelationId` + application properties |
| Worker | Start logging scope with `tenantId` + `correlationId` from envelope |
| DB writes | Write `CorrelationId` into domain rows |

This enables end-to-end tracing: API request -> queue message -> worker processing -> database state.
