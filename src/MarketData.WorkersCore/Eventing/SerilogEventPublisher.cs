using Core;
using Microsoft.Extensions.Logging;

namespace MarketData.Workers;

/// <summary>
/// Bare-bones <see cref="IEventPublisher"/> that emits each <see cref="BaseEvent"/> as a structured
/// log record. Replace with a real transport implementation behind <see cref="IEventPublisher"/>.
/// </summary>
public sealed class SerilogEventPublisher : IEventPublisher
{
    private readonly ILogger<SerilogEventPublisher> _logger;

    public SerilogEventPublisher(ILogger<SerilogEventPublisher> logger) => _logger = logger;

    public Task PublishAsync(BaseEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Event {EventClass}/{EventSubclass} {EventName} from {EventContext}: {EventBody}",
            @event.Class, @event.Subclass, @event.Name, @event.Context, @event.Body);
        return Task.CompletedTask;
    }
}
