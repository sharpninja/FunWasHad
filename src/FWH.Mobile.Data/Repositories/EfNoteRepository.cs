using Microsoft.EntityFrameworkCore;
using FWH.Mobile.Data.Data;
using FWH.Mobile.Data.Models;

namespace FWH.Mobile.Data.Repositories;

public class EfNoteRepository : INoteRepository
{
    private readonly NotesDbContext _context;

    public EfNoteRepository(NotesDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default)
    {
        if (note == null)
            throw new ArgumentNullException(nameof(note));

        try
        {
            var entry = await _context.Notes.AddAsync(note, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entry.Entity;
        }
        catch (DbUpdateException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    public async Task<Note?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Notes.FindAsync(new object[] { id }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    public async Task<IEnumerable<Note>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.Notes.OrderByDescending(n => n.CreatedAt).ToListAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default)
    {
        if (note == null)
            throw new ArgumentNullException(nameof(note));

        try
        {
            _context.Notes.Update(note);
            var affected = await _context.SaveChangesAsync(cancellationToken);
            return affected > 0;
        }
        catch (DbUpdateException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _context.Notes.FindAsync(new object[] { id }, cancellationToken);
            if (entity == null) return false;
            _context.Notes.Remove(entity);
            var affected = await _context.SaveChangesAsync(cancellationToken);
            return affected > 0;
        }
        catch (DbUpdateException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }
}
