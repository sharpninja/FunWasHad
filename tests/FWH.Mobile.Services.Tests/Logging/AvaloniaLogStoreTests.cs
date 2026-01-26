using FWH.Mobile.Logging;
using Xunit;

namespace FWH.Mobile.Services.Tests.Logging;

public sealed class AvaloniaLogStoreTests
{
    /// <summary>
    /// Tests that AvaloniaLogStore constructor succeeds with a valid maximum entries value and initializes with an empty entries collection.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The AvaloniaLogStore constructor's ability to create an instance with valid configuration parameters.</para>
    /// <para><strong>Data involved:</strong> A maxEntries value of 100, which represents the maximum number of log entries the store will retain before removing oldest entries. This is a reasonable value for a log viewer UI component.</para>
    /// <para><strong>Why the data matters:</strong> The log store must have a maximum capacity to prevent unbounded memory growth. When the limit is reached, older entries should be removed (FIFO). The initial state should be empty, ready to accept new log entries. This test validates basic construction and initialization.</para>
    /// <para><strong>Expected outcome:</strong> The constructor should complete without throwing exceptions, the store instance should not be null, and the Entries collection should be empty initially.</para>
    /// <para><strong>Reason for expectation:</strong> With valid input (positive maxEntries), the constructor should successfully create the store and initialize it with an empty collection. The empty Entries confirms the store starts in a clean state, ready to accept log entries. This is a basic sanity check for object construction.</para>
    /// </remarks>
    [Fact]
    public void ConstructorWithValidMaxEntriesSucceeds()
    {
        var store = new AvaloniaLogStore(maxEntries: 100);

        Assert.NotNull(store);
        Assert.Empty(store.Entries);
    }

    [Fact]
    public void Constructor_WithZeroMaxEntries_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AvaloniaLogStore(maxEntries: 0));
    }

    [Fact]
    public void Constructor_WithNegativeMaxEntries_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AvaloniaLogStore(maxEntries: -1));
    }

    [Fact]
    public void AddWithNullEntryThrowsArgumentNullException()
    {
        var store = new AvaloniaLogStore(maxEntries: 10);

        Assert.Throws<ArgumentNullException>(() => store.Add(null!));
    }
}
