# BillerJacket -- Support Playbook

## Overview

This playbook covers common support scenarios for operating BillerJacket in production. All operations assume access to the Razor Pages admin UI, Azure Portal, and Application Insights.

---

## Tracing a Request (Correlation ID)

Every API request, queue message, and database write carries a `CorrelationId`. This is the primary tool for debugging.

### Finding the Correlation ID

| Source | Where to Look |
|--------|---------------|
| API response | Returned in response body or `X-Correlation-Id` header |
| Service Bus message | `ServiceBusMessage.CorrelationId` property |
| Database records | `CorrelationId` column on Payment, PaymentAttempt, WebhookEvent, CommunicationLog, AuditLog |
| Application Insights | Custom dimension `correlationId` in traces |

### Tracing End-to-End

1. Start with the CorrelationId from the API response or customer report
2. Query Application Insights:
   ```kql
   traces
   | where customDimensions.correlationId == "<id>"
   | order by timestamp asc
   ```
3. Query database for related records:
   ```sql
   SELECT * FROM AuditLogs WHERE CorrelationId = '<id>' ORDER BY OccurredAt;
   SELECT * FROM CommunicationLogs WHERE CorrelationId = '<id>';
   SELECT * FROM PaymentAttempts WHERE CorrelationId = '<id>';
   ```

---

## Invoice Timeline Reconstruction

To understand everything that happened to an invoice:

```sql
-- Invoice details
SELECT * FROM Invoices WHERE InvoiceId = '<id>';

-- Line items
SELECT * FROM InvoiceLineItems WHERE InvoiceId = '<id>' ORDER BY LineNumber;

-- Payments applied
SELECT * FROM Payments WHERE InvoiceId = '<id>' ORDER BY AppliedAt;

-- Payment attempts (including failures)
SELECT * FROM PaymentAttempts WHERE InvoiceId = '<id>' ORDER BY AttemptedAt;

-- Communications sent
SELECT * FROM CommunicationLogs WHERE InvoiceId = '<id>' ORDER BY SentAt;

-- Dunning state
SELECT * FROM InvoiceDunningStates WHERE InvoiceId = '<id>';

-- Audit trail
SELECT * FROM AuditLogs
WHERE EntityType = 'Invoice' AND EntityId = '<id>'
ORDER BY OccurredAt;
```

---

## Dead-Letter Queue (DLQ) Management

### Viewing DLQ Messages

The Razor Pages UI at `/support/dlq` shows dead-lettered messages across all queues.

In Azure Portal:
1. Navigate to Service Bus namespace `sb-billerjacket-{env}`
2. Select the queue (e.g., `email-send`)
3. Click "Dead-letter queue" tab
4. View messages with their dead-letter reason

### Common DLQ Reasons

| Reason | Meaning | Action |
|--------|---------|--------|
| `bad_envelope` | Message body couldn't be deserialized | Check publisher serialization. Usually a bug -- fix and redeploy. |
| `unknown_message_type` | MessageType string not recognized by consumer | Version mismatch between publisher and consumer. Deploy consumer first. |
| `processing_failed` | Non-transient exception during handling | Check ErrorMessage. Fix root cause, then replay. |
| (auto) MaxDeliveryCount exceeded | Transient failures exhausted all retries | Investigate underlying service (SQL, email provider). Replay after fix. |

### Replaying a DLQ Message

**Via UI:** Navigate to `/support/dlq`, find the message, click "Replay."

**Via Azure Portal:**
1. Open the DLQ for the queue
2. Peek the message to inspect its content
3. Copy the message body
4. Re-publish to the main queue (Service Bus Explorer or code)

**Via API:** `POST /api/webhooks/{id}/replay` for webhook events.

### Safety

All message handlers are idempotent. Replaying a message that was already partially processed will not cause double-processing (idempotency keys, webhook deduplication, etc.).

---

## Webhook Debugging

### Webhook Not Processing

1. Check `WebhookEvents` table:
   ```sql
   SELECT * FROM WebhookEvents
   WHERE WebhookEventId = '<id>';
   ```
2. Check `ProcessingStatus`:
   - `Received`: Message was stored but worker hasn't picked it up yet. Check `webhook-ingest` queue depth.
   - `Processing`: Worker is actively handling it. If stuck, check for long-running operations.
   - `Failed`: Check `ErrorMessage` column for details.
   - `Processed`: Completed successfully.

3. Check Application Insights for the CorrelationId:
   ```kql
   traces
   | where customDimensions.correlationId == "<correlationId>"
   | where customDimensions.messageType == "webhook.received"
   | order by timestamp asc
   ```

### Replaying a Webhook

**Via UI:** Navigate to `/support/webhooks`, find the event, click "Replay."

**Via API:** `POST /api/webhooks/{id}/replay`

This publishes a `WebhookReplayRequested` message. The worker reprocesses the stored payload idempotently.

---

## Payment Investigation

### Payment Not Applied

1. Check idempotency key:
   ```sql
   SELECT * FROM IdempotencyKeys
   WHERE TenantId = '<tenantId>'
     AND Operation = 'ApplyPayment'
     AND KeyValue = '<idempotencyKey>';
   ```
   If a row exists, the payment was already processed (check `ResponseJson`).

2. Check payment attempts:
   ```sql
   SELECT * FROM PaymentAttempts
   WHERE InvoiceId = '<invoiceId>'
   ORDER BY AttemptedAt DESC;
   ```
   Look at `Status`, `FailureCode`, `FailureMessage`.

3. Check the `payment-commands` queue:
   - Is the message still in the queue (not yet processed)?
   - Is it in the DLQ?

4. Check Application Insights:
   ```kql
   traces
   | where customDimensions.feature == "Payment"
   | where customDimensions.correlationId == "<correlationId>"
   ```

### Double Payment Concern

Idempotency keys prevent double processing. To verify:
1. Query `IdempotencyKeys` for the key value
2. Check that only one `Payment` record exists for that idempotency key
3. Verify `Invoice.PaidAmount` matches the sum of `Payments.Amount`

---

## Dunning Issues

### Dunning Not Sending Reminders

1. Verify the tenant has an active dunning plan:
   ```sql
   SELECT * FROM DunningPlans
   WHERE TenantId = '<tenantId>' AND IsActive = 1;
   ```

2. Check the invoice has a dunning state:
   ```sql
   SELECT * FROM InvoiceDunningStates
   WHERE InvoiceId = '<invoiceId>';
   ```
   If no row exists, the daily evaluation hasn't run or the invoice wasn't overdue when it ran.

3. Check `NextActionAt`:
   - If in the future: the next reminder is scheduled, not due yet.
   - If in the past: the evaluation job may not have run. Check `dunning-evaluate` queue and worker logs.

4. Check `CommunicationLogs` for sent reminders:
   ```sql
   SELECT * FROM CommunicationLogs
   WHERE InvoiceId = '<invoiceId>' AND Type = 'Dunning'
   ORDER BY SentAt;
   ```

### Dunning Sent Too Many Reminders

Check `CurrentStepNumber` against the plan's total steps. The dunning system should not exceed the plan's step count. If it did, check for duplicate `EvaluateDunningCommand` processing (look at CorrelationIds in logs).

---

## Email Delivery Issues

### Email Not Sent

1. Check `CommunicationLogs`:
   ```sql
   SELECT * FROM CommunicationLogs
   WHERE InvoiceId = '<invoiceId>' AND Channel = 'Email'
   ORDER BY SentAt DESC;
   ```

2. If status is `Failed`: check `ErrorMessage` for provider error details.

3. If no row exists: the email message may still be in the `email-send` queue or DLQ.

4. Check `email-send` queue depth and DLQ in Azure Portal.

---

## Application Insights KQL Queries

### Recent Errors by Feature

```kql
traces
| where severityLevel >= 3
| where customDimensions.feature != ""
| summarize count() by tostring(customDimensions.feature),
    tostring(customDimensions.operation)
| order by count_ desc
```

### Slow Queue Processing

```kql
traces
| where customDimensions.component == "Worker"
| where customDimensions.operation == "ProcessMessage"
| extend duration = todouble(customDimensions.durationMs)
| where duration > 5000
| project timestamp, customDimensions.messageType, duration,
    customDimensions.correlationId
| order by duration desc
```

### Failed Payments This Week

```kql
traces
| where customDimensions.feature == "Payment"
| where customDimensions.operation == "ApplyPayment"
| where severityLevel >= 3
| where timestamp > ago(7d)
| project timestamp, customDimensions.correlationId,
    customDimensions.tenantId, message
```

### Dunning Activity

```kql
traces
| where customDimensions.feature == "Dunning"
| where timestamp > ago(1d)
| summarize count() by tostring(customDimensions.operation),
    tostring(customDimensions.tenantId)
```

---

## Operational Checklist

### Daily

- [ ] Check DLQ message count across all queues
- [ ] Verify dunning evaluation ran (check logs for `EvaluateDunningCommand`)
- [ ] Review failed payments in the last 24 hours

### Weekly

- [ ] Review `WebhookEvents` with `ProcessingStatus = 'Failed'`
- [ ] Check aging report for invoices overdue > 90 days
- [ ] Review Application Insights for error trends

### On Incident

1. Get the CorrelationId
2. Trace through Application Insights
3. Query related database tables
4. Check DLQ for related messages
5. Replay if safe (all handlers are idempotent)
6. Document findings in incident log
