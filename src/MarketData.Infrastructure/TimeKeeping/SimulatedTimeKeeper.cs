using MarketData.Application.Contracts;

namespace MarketData.Infrastructure.TimeKeeping;

/// <summary>
/// Deterministic <see cref="ITimeKeeper"/> for tests and Sim-mode runs.
///
/// Time never advances on its own — there is no wall-clock ticking. It moves only
/// when a caller invokes <see cref="SetTime"/> or the <see cref="Advance"/> helper.
/// This is what makes TIF / GTD expiry and latency simulation reproducible: a test
/// advances the clock explicitly and asserts on the result, with no sleeps.
///
/// <see cref="WaitTime"/> returns a task that completes the moment the clock is moved
/// to (or past) the requested instant, so code awaiting a future time is released
/// synchronously by the same <see cref="SetTime"/>/<see cref="Advance"/> call that
/// crosses it — keeping the whole simulation single-stepped and deterministic.
/// </summary>
public sealed class SimulatedTimeKeeper : ITimeKeeper
{
    private readonly object _gate = new();
    private DateTimeOffset _now;

    // Pending WaitTime callers, ordered is not required; we scan on each SetTime.
    private readonly List<Waiter> _waiters = [];

    /// <summary>
    /// Create a keeper anchored at <paramref name="start"/>, or at the current UTC
    /// wall-clock time if none is given (a one-time read — the clock does not tick after).
    /// </summary>
    public SimulatedTimeKeeper(DateTimeOffset? start = null)
    {
        _now = start ?? DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public DateTimeOffset Now
    {
        get { lock (_gate) return _now; }
    }

    /// <inheritdoc />
    public Task SetTime(DateTimeOffset time)
    {
        List<Waiter>? released = null;

        lock (_gate)
        {
            // Time only moves forward; ignore attempts to rewind.
            if (time > _now)
                _now = time;

            for (int i = _waiters.Count - 1; i >= 0; i--)
            {
                if (_waiters[i].Target <= _now)
                {
                    (released ??= []).Add(_waiters[i]);
                    _waiters.RemoveAt(i);
                }
            }
        }

        // Complete outside the lock so continuations can't deadlock on _gate.
        if (released is not null)
            foreach (var w in released)
                w.Completion.TrySetResult();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WaitTime(DateTimeOffset time)
    {
        lock (_gate)
        {
            if (_now >= time)
                return Task.CompletedTask;

            var waiter = new Waiter(time);
            _waiters.Add(waiter);
            return waiter.Completion.Task;
        }
    }

    /// <summary>
    /// Convenience: advance the clock by <paramref name="span"/> from the current time.
    /// Equivalent to <c>SetTime(Now + span)</c>.
    /// </summary>
    public Task Advance(TimeSpan span) => SetTime(Now + span);

    private sealed class Waiter(DateTimeOffset target)
    {
        public DateTimeOffset Target { get; } = target;

        public TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
