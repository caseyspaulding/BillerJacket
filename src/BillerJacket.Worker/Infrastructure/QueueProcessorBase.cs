using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BillerJacket.Contracts.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BillerJacket.Worker.Infrastructure;

public abstract class QueueProcessorBase : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private readonly string _queueName;

    protected QueueProcessorBase(
        ServiceBusClient client,
        IServiceScopeFactory scopeFactory,
        ILogger logger,
        string queueName)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _queueName = queueName;
    }

    protected abstract Task HandleAsync(MessageEnvelope envelope, IServiceProvider scopedServices, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        processor.ProcessMessageAsync += async args =>
        {
            MessageEnvelope? envelope = null;
            try
            {
                envelope = JsonSerializer.Deserialize<MessageEnvelope>(
                    args.Message.Body.ToString(), JsonDefaults.Options);

                if (envelope is null)
                {
                    _logger.LogError("Failed to deserialize envelope from {Queue}", _queueName);
                    await args.DeadLetterMessageAsync(args.Message, "InvalidPayload",
                        "Could not deserialize message envelope", stoppingToken);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();

                var tenantProvider = scope.ServiceProvider.GetRequiredService<MessageScopeTenantProvider>();
                if (Guid.TryParse(envelope.TenantId, out var tenantId))
                    tenantProvider.TenantId = tenantId;

                using var logScope = _logger.BeginScope(new Dictionary<string, object?>
                {
                    ["TenantId"] = envelope.TenantId,
                    ["CorrelationId"] = envelope.CorrelationId,
                    ["MessageType"] = envelope.MessageType,
                    ["Queue"] = _queueName
                });

                _logger.LogInformation("Processing {MessageType} from {Queue}", envelope.MessageType, _queueName);

                await HandleAsync(envelope, scope.ServiceProvider, stoppingToken);

                await args.CompleteMessageAsync(args.Message, stoppingToken);

                _logger.LogInformation("Completed {MessageType} from {Queue}", envelope.MessageType, _queueName);
            }
            catch (DeadLetterException ex)
            {
                _logger.LogWarning(ex, "Dead-lettering message from {Queue}: {Reason}", _queueName, ex.Reason);
                await args.DeadLetterMessageAsync(args.Message, "ProcessingFailed", ex.Reason, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message from {Queue} (type={MessageType})",
                    _queueName, envelope?.MessageType ?? "unknown");
                await args.AbandonMessageAsync(args.Message, cancellationToken: stoppingToken);
            }
        };

        processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "Service Bus error on {Queue}: {Source}",
                _queueName, args.ErrorSource);
            return Task.CompletedTask;
        };

        _logger.LogInformation("Starting queue processor for {Queue}", _queueName);
        await processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested
        }

        _logger.LogInformation("Stopping queue processor for {Queue}", _queueName);
        await processor.StopProcessingAsync();
    }
}
