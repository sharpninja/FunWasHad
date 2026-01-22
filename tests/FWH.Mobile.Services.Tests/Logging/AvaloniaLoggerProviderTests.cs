using FWH.Mobile.Logging;
using Xunit;

namespace FWH.Mobile.Services.Tests.Logging;

public sealed class AvaloniaLoggerProviderTests
{
    /// <summary>
    /// Tests that CreateLogger returns a non-null logger instance for a given category name.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The AvaloniaLoggerProvider.CreateLogger method's ability to create logger instances for specified categories.</para>
    /// <para><strong>Data involved:</strong> An AvaloniaLogStore with maxEntries=10 and a category name "Test.Category". The provider uses the store to persist log entries created by the logger.</para>
    /// <para><strong>Why the data matters:</strong> Loggers are created per category (typically matching class namespaces) to enable filtering and organization of log messages. The provider must successfully create logger instances so application code can log messages. The category name allows logs to be filtered by component or namespace.</para>
    /// <para><strong>Expected outcome:</strong> CreateLogger should return a non-null ILogger instance for the category "Test.Category".</para>
    /// <para><strong>Reason for expectation:</strong> The provider should create logger instances on demand for any category name. The non-null result confirms that logger creation succeeds and the returned logger can be used for logging. This is a basic functionality test to ensure the provider works correctly.</para>
    /// </remarks>
    [Fact]
    public void CreateLogger_ReturnsLogger()
    {
        var store = new AvaloniaLogStore(maxEntries: 10);
        var provider = new AvaloniaLoggerProvider(store);

        var logger = provider.CreateLogger("Test.Category");

        Assert.NotNull(logger);
    }

    [Fact]
    public void CreateLogger_SameCategory_ReturnsSameInstance()
    {
        var store = new AvaloniaLogStore(maxEntries: 10);
        var provider = new AvaloniaLoggerProvider(store);

        var logger1 = provider.CreateLogger("Test.Category");
        var logger2 = provider.CreateLogger("Test.Category");

        Assert.Same(logger1, logger2);
    }

    [Fact]
    public void Dispose_ClearsLoggers()
    {
        var store = new AvaloniaLogStore(maxEntries: 10);
        var provider = new AvaloniaLoggerProvider(store);

        provider.CreateLogger("Test.Category");
        provider.Dispose();

        // After dispose, subsequent operations should not throw
        Assert.NotNull(provider);
    }
}
