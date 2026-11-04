// @Role: Zegar testowy z automatycznym przesuwaniem: Task.Delay(…, TimeProvider) kończy się natychmiast i przesuwa czas o żądany odstęp, więc test nie czeka realnie 3 s + 2 s Mocka, a znaczniki czasu i czas systemowy są deterministyczne | @DependsOn: System.TimeProvider | @UsedBy: WorkerTestowy, Solutions/SolutionsFlowTests | @Task: TASK-128
namespace Lemat.Api.Tests.Support;

public sealed class ZegarTestowy : TimeProvider
{
    private readonly object _blokada = new();
    private DateTimeOffset _teraz;
    private long _znacznik;

    public ZegarTestowy(DateTimeOffset start) => _teraz = start;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override DateTimeOffset GetUtcNow()
    {
        lock (_blokada)
        {
            return _teraz;
        }
    }

    public override long GetTimestamp()
    {
        lock (_blokada)
        {
            return _znacznik;
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        ArgumentNullException.ThrowIfNull(callback);
        if (dueTime != Timeout.InfiniteTimeSpan)
        {
            lock (_blokada)
            {
                _teraz += dueTime;
                _znacznik += dueTime.Ticks;
            }

            ThreadPool.QueueUserWorkItem(_ => callback(state));
        }

        return new Licznik();
    }

    private sealed class Licznik : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
