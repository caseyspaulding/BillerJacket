using BillerJacket.Application.Common;

namespace BillerJacket.Worker.Infrastructure;

public class MessageScopeTenantProvider : ITenantProvider
{
    public Guid? TenantId { get; set; }
}
