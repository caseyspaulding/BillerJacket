using System.Text.Json;
using BillerJacket.Api.Models;
using BillerJacket.Application.Common;
using BillerJacket.Contracts.Messaging;
using BillerJacket.Domain.Entities;
using BillerJacket.Infrastructure.Data;
using BillerJacket.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillerJacket.Api.Controllers;

[ApiController]
[Route("api/dunning")]
[Authorize]
public class DunningController : ControllerBase
{
    private readonly ArDbContext _db;
    private readonly IBusPublisher _bus;
    private readonly LoggingContext _logging;

    public DunningController(ArDbContext db, IBusPublisher bus, ILogger<DunningController> logger)
    {
        _db = db;
        _bus = bus;
        _logging = new LoggingContext(logger);
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken ct)
    {
        var tenantId = Current.TenantId;
        var correlationId = Current.CorrelationId;

        using var _ = _logging.WithContext(
            feature: "Dunning",
            operation: "TriggerRun",
            component: "API",
            tenantId: tenantId);

        await _bus.PublishAsync(Queues.DunningEvaluate, new EvaluateDunningCommand(
            TenantId: tenantId.ToString(),
            CorrelationId: correlationId,
            AsOfDate: DateOnly.FromDateTime(DateTime.UtcNow),
            ExternalSource: null,
            ExternalReferenceId: null,
            RequestedByUserId: Current.UserIdOrNull?.ToString(),
            OccurredAt: DateTimeOffset.UtcNow), ct);

        _db.AuditLogs.Add(new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            TenantId = tenantId,
            EntityType = "Dunning",
            EntityId = tenantId.ToString(),
            Action = "dunning.run_triggered",
            DataJson = JsonSerializer.Serialize(new { AsOfDate = DateOnly.FromDateTime(DateTime.UtcNow) }),
            PerformedByUserId = Current.UserIdOrNull,
            OccurredAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId
        });

        await _db.SaveChangesAsync(ct);

        return Accepted(new DunningRunResponse("queued", correlationId));
    }
}
