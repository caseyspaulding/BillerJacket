namespace BillerJacket.Application.Common;

public class CurrentTenantProvider : ITenantProvider
{
    public Guid? TenantId => Current.TenantIdOrNull;
}
