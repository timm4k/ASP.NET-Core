using System.Collections.Concurrent;
using CoffeeOrdering.Contracts;
using CoffeeOrdering.Models;

namespace CoffeeOrdering.Services;

public sealed class NoteStore(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<Guid, CoffeeNote> _notes = new();

    public IReadOnlyList<NoteResponse> GetAll(Guid ownerId) => _notes.Values
        .Where(note => note.OwnerId == ownerId)
        .OrderByDescending(note => note.UpdatedAt)
        .Select(ToResponse)
        .ToArray();

    public NoteResponse? Get(Guid id, Guid ownerId) =>
        _notes.TryGetValue(id, out CoffeeNote? note) && note.OwnerId == ownerId ? ToResponse(note) : null;

    public NoteResponse Create(Guid ownerId, string title, string content)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        CoffeeNote note = new(Guid.NewGuid(), ownerId, title.Trim(), content.Trim(), now, now);
        _notes[note.Id] = note;
        return ToResponse(note);
    }

    public NoteResponse? Update(Guid id, Guid ownerId, string title, string content)
    {
        while (_notes.TryGetValue(id, out CoffeeNote? current))
        {
            if (current.OwnerId != ownerId)
            {
                return null;
            }

            CoffeeNote updated = current with
            {
                Title = title.Trim(),
                Content = content.Trim(),
                UpdatedAt = timeProvider.GetUtcNow()
            };

            if (_notes.TryUpdate(id, updated, current))
            {
                return ToResponse(updated);
            }
        }

        return null;
    }

    public bool Delete(Guid id, Guid ownerId)
    {
        return _notes.TryGetValue(id, out CoffeeNote? note)
            && note.OwnerId == ownerId
            && _notes.TryRemove(new KeyValuePair<Guid, CoffeeNote>(id, note));
    }

    private static NoteResponse ToResponse(CoffeeNote note) =>
        new(note.Id, note.Title, note.Content, note.CreatedAt, note.UpdatedAt);
}
