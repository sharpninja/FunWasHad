using FWH.Mobile.Data.Models;

namespace FWH.Mobile.Data.Repositories;

public interface INoteRepository
{
    Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default);
    Task<Note?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Note>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
