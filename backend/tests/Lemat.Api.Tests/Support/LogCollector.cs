// @Role: Dostawca logów zbierający komunikaty, stany i wyjątki w pamięci (test: brak haseł i tokenów w logach) | @DependsOn: Microsoft.Extensions.Logging | @UsedBy: ApiFactory, Accounts/AuthenticationTests | @Task: TASK-123
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Lemat.Api.Tests.Support;

public sealed class KolektorLogow : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _wpisy = new();

    public string Calosc => string.Join('\n', _wpisy);

    public ILogger CreateLogger(string categoryName) => new Zapis(categoryName, _wpisy);

    public void Dispose()
    {
    }

    private sealed class Zapis(string kategoria, ConcurrentQueue<string> wpisy) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            var stan = state is IEnumerable<KeyValuePair<string, object?>> pary
                ? string.Join(';', pary.Select(p => $"{p.Key}={p.Value}"))
                : string.Empty;
            wpisy.Enqueue($"{kategoria}|{logLevel}|{formatter(state, exception)}|{stan}|{exception}");
        }
    }
}
