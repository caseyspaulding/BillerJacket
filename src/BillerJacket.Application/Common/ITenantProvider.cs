namespace BillerJacket.Application.Common;

public interface ITenantProvider
{
    Guid? TenantId { get; }
}
