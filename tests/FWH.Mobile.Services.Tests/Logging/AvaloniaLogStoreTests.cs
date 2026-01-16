using System;
using Xunit;
using FWH.Mobile.Logging;

namespace FWH.Mobile.Services.Tests.Logging;

public sealed class AvaloniaLogStoreTests
{
    [Fact]
    public void Constructor_WithValidMaxEntries_Succeeds()
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
    public void Add_WithNullEntry_ThrowsArgumentNullException()
    {
        var store = new AvaloniaLogStore(maxEntries: 10);

        Assert.Throws<ArgumentNullException>(() => store.Add(null!));
    }
}
