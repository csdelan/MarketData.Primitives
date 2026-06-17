using Core;

namespace MarketData.Workers;

/// <summary>
/// Transport-agnostic publisher for <see cref="BaseEvent"/>s emitted by the worker host
/// (job lifecycle, heartbeats). The bare-bones host ships a Serilog-backed implementation; a real
/// transport (queue, ZMQ, etc.) can be dropped in behind this seam without touching callers.
/// </summary>
public interface IEventPublisher
{
    /// <summary>Publishes an event. Implementations must not throw for transport faults.</summary>
    Task PublishAsync(BaseEvent @event, CancellationToken cancellationToken = default);
}
