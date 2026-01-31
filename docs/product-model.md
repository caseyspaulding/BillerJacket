# BillerJacket -- Product Model

## What Is BillerJacket?

BillerJacket is an Accounts Receivable (AR) automation service that handles invoices, payments, autopay, dunning/collections, and auditability tooling. It operates as a standalone internal service consumed by RoofingJacket (initial client) and future vertical products.

## Core Principle

> **BillerJacket owns money. RoofingJacket owns work.**

- No shared databases.
- No shared domain models.
- Only well-defined service contracts.

## Target Market

BillerJacket is purpose-built for trades and service businesses where:

1. Invoices are job-based
2. Payments are delayed or partial
3. Customers don't pay immediately
4. QuickBooks is already used
5. Owners hate chasing money

### Best-Fit Verticals

| Segment | Examples | Fit |
|---------|----------|-----|
| Milestone/Progress Billing | General contractors, concrete, window/siding, remodelers | Top-tier |
| Service + Project Hybrid | HVAC, plumbing, electrical | Very strong |
| Commercial B2B Services | Commercial cleaning, elevator service, industrial maintenance | Excellent |
| Construction-Adjacent | Fire protection, steel fabricators | Strong |
| High-Ticket Consumer | Solar installers, fence companies, paving/asphalt | Strong |

### Where This Doesn't Fit

- Landscapers (pay-on-completion)
- Handymen
- Pressure washing
- Solo operators with small tickets
- Cash-first trades

## Product Scope

BillerJacket is not a generic payment processor and not a loan system.

> Track money owed, collect it reliably, and explain what happened when something goes wrong.

### In Scope

- Multi-tenant platform with role-based access
- Customer management with external reference linking
- Invoice creation, lifecycle, and delivery
- Manual and idempotent payment processing
- Autopay enrollment and scheduled charges (foundational)
- Dunning and collections automation
- Communication logging (email/SMS)
- Webhook ingestion, normalization, and replay
- Immutable audit logs
- Support and operations tooling (DLQ, replay, trace)
- Dashboard and reporting (aging, balances, success rates)
- SuperAdmin administration (tenants, blog, landing pages)

### Explicitly Out of Scope

- Lending / interest calculation
- Credit underwriting
- Consumer wallet
- Payment processor replacement
- Accounting general ledger (full GAAP)

## Feature Phasing

**V1 (Portfolio + Real Value)**
- Customers, invoices (one-time + installments), manual payments (idempotent), dunning (email reminders), activity log, dashboard, SuperAdmin administration

**V1.5**
- Autopay, webhook replay UI, aging reports

**V2 (If it earns it)**
- Multiple currencies, accounting system sync, advanced dunning strategies, payment provider integrations

## RoofingJacket Integration

RoofingJacket sends commands (`CreateInvoice`, `SendInvoice`) and queries billing state (`BillingSummary`, `InvoiceStatus`, `PaymentHistory`). RoofingJacket displays billing state but **never mutates billing data**.
