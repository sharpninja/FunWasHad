using System.Threading.Tasks;
using Xunit;
using FWH.Mobile.Data.Models;
using FWH.Mobile.Data.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace FWH.Mobile.Data.Tests;

public class NoteRepositoryTests : DataTestBase
{
    [Fact]
    public async Task CreateAndRetrieveNote()
    {
        var repo = ServiceProvider.GetRequiredService<INoteRepository>();
        var note = new Note { Title = "T1", Content = "C1" };
        var created = await repo.CreateAsync(note);
        Assert.True(created.Id > 0);

        var got = await repo.GetByIdAsync(created.Id);
        Assert.NotNull(got);
        Assert.Equal("T1", got!.Title);
    }
}
