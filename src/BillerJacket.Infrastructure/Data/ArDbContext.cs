using BillerJacket.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BillerJacket.Infrastructure.Data;

public class ArDbContext : DbContext
{
    private readonly Guid? _tenantId;

    public ArDbContext(DbContextOptions<ArDbContext> options) : base(options) { }

    public ArDbContext(DbContextOptions<ArDbContext> options, Guid tenantId)
        : base(options)
    {
        _tenantId = tenantId;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<DunningPlan> DunningPlans => Set<DunningPlan>();
    public DbSet<DunningStep> DunningSteps => Set<DunningStep>();
    public DbSet<InvoiceDunningState> InvoiceDunningStates => Set<InvoiceDunningState>();
    public DbSet<CommunicationLog> CommunicationLogs => Set<CommunicationLog>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApiKeyRecord> ApiKeys => Set<ApiKeyRecord>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<LandingPage> LandingPages => Set<LandingPage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArDbContext).Assembly);

        // Global query filters for tenant isolation
        if (_tenantId.HasValue)
        {
            var tid = _tenantId.Value;
            modelBuilder.Entity<Customer>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<Invoice>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<InvoiceLineItem>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<Payment>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<PaymentAttempt>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<IdempotencyKey>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<DunningPlan>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<DunningStep>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<InvoiceDunningState>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<CommunicationLog>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<WebhookEvent>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.TenantId == tid);
            modelBuilder.Entity<ApiKeyRecord>().HasQueryFilter(e => e.TenantId == tid);
        }
    }
}
