using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace e2e.Tests.Shared;

public static class TestLogger
{
    private static readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder
        .AddSimpleConsole(options =>
        {
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.IncludeScopes = false;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        })
        .SetMinimumLevel(LogLevel.Information));

    public static ILogger<T> Create<T>() => _loggerFactory.CreateLogger<T>();
    public static ILogger Create(string categoryName) => _loggerFactory.CreateLogger(categoryName);
}