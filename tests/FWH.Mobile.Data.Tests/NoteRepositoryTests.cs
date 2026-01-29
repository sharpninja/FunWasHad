using FWH.Mobile.Data.Models;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FWH.Mobile.Data.Tests;

public class NoteRepositoryTests : DataTestBase
{
    /// <summary>
    /// Tests that a note can be created and subsequently retrieved from the repository with matching data.
    /// </summary>
    /// <remarks>
    /// <para><strong>What is being tested:</strong> The INoteRepository's CreateAsync and GetByIdAsync methods for persisting and retrieving note entities.</para>
    /// <para><strong>Data involved:</strong> A Note entity with Title="T1" and Content="C1". The note is created via CreateAsync, which should assign a database-generated Id, and then retrieved using that Id.</para>
    /// <para><strong>Why the data matters:</strong> Notes are simple entities with title and content. This test validates basic CRUD operations work correctly - creating a note should persist it to the database and assign an Id, and retrieving by Id should return the same note with matching title and content. The test data is minimal to focus on the core persistence functionality.</para>
    /// <para><strong>Expected outcome:</strong> After creating the note, the returned note should have an Id > 0 (database-generated), and GetByIdAsync should return a note with matching Id and Title="T1".</para>
    /// <para><strong>Reason for expectation:</strong> The repository should persist the note to the database, and the database should assign a positive integer Id (typically auto-incrementing). When retrieving by Id, the repository should return the exact same note that was created, with all properties (Title, Content) matching. This validates the complete create-and-retrieve round-trip works correctly.</para>
    /// </remarks>
    [Fact]
    public async Task CreateAndRetrieveNote()
    {
        var repo = ServiceProvider.GetRequiredService<INoteRepository>();
        var note = new Note { Title = "T1", Content = "C1" };
        var created = await repo.CreateAsync(note).ConfigureAwait(true);
        Assert.True(created.Id > 0);

        var got = await repo.GetByIdAsync(created.Id).ConfigureAwait(true);
        Assert.NotNull(got);
        Assert.Equal("T1", got!.Title);
    }
}
