using Microsoft.Extensions.Logging;

namespace BillerJacket.Application.Common;

public class LoggingContext
{
    private readonly ILogger _logger;

    public LoggingContext(ILogger logger)
    {
        _logger = logger;
    }

    public IDisposable? WithContext(
        string feature,
        string operation,
        string component,
        Guid? tenantId = null,
        string? jobId = null)
    {
        var state = new Dictionary<string, object?>
        {
            ["Feature"] = feature,
            ["Operation"] = operation,
            ["Component"] = component,
            ["CorrelationId"] = Current.CorrelationId
        };

        if (tenantId.HasValue)
            state["TenantId"] = tenantId.Value;
        if (jobId is not null)
            state["JobId"] = jobId;

        return _logger.BeginScope(state);
    }
}
