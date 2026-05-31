using System.Collections.Concurrent;

namespace Borderland.Dreams.Utils;

public record LogEntry(DateTimeOffset Timestamp, LogLevel Level, string Category, string Message);

public class InMemoryLogStore
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    public IEnumerable<LogEntry> Entries => _entries;

    public void Add(LogEntry entry)
    {
        _entries.Enqueue(entry);
        // Optional: cap size to avoid unbounded growth
        while (_entries.Count > 1000)
            _entries.TryDequeue(out _);
    }
}

public class InMemoryLoggerProvider(InMemoryLogStore store) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new InMemoryLogger(categoryName, store);

    public void Dispose() { }
}

public class InMemoryLogger(string category, InMemoryLogStore store) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        store.Add(new LogEntry(DateTimeOffset.UtcNow, logLevel, category, formatter(state, exception)));
    }
}