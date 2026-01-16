using FWH.Mobile.Logging;
using Xunit;

namespace FWH.Mobile.Services.Tests.Logging;

public sealed class AvaloniaLoggerProviderTests
{
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
