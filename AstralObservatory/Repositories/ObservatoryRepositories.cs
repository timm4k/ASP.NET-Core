using AstralObservatory.Data;
using AstralObservatory.Models;
using Microsoft.EntityFrameworkCore;

namespace AstralObservatory.Repositories;

public interface ICelestialObjectRepository
{
    Task<List<CelestialObject>> GetAllAsync(CancellationToken cancellationToken);
    Task<CelestialObject?> GetAsync(int id, CancellationToken cancellationToken);
}

public sealed class CelestialObjectRepository(AppDbContext db) : ICelestialObjectRepository
{
    public Task<List<CelestialObject>> GetAllAsync(CancellationToken cancellationToken) =>
        db.CelestialObjects.AsNoTracking().OrderBy(item => item.Id).ToListAsync(cancellationToken);

    public Task<CelestialObject?> GetAsync(int id, CancellationToken cancellationToken) =>
        db.CelestialObjects.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
}

public interface IObservationRepository
{
    Task<List<Observation>> GetHistoryAsync(int celestialObjectId, int count, CancellationToken cancellationToken);
}

public sealed class ObservationRepository(AppDbContext db) : IObservationRepository
{
    public Task<List<Observation>> GetHistoryAsync(int celestialObjectId, int count, CancellationToken cancellationToken) =>
        db.Observations
            .AsNoTracking()
            .Where(item => item.CelestialObjectId == celestialObjectId)
            .OrderByDescending(item => item.Id)
            .Take(count)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);
}

public interface IObservationAlertRepository
{
    Task<List<ObservationAlert>> GetForUserAsync(string userId, CancellationToken cancellationToken);
    Task AddAsync(ObservationAlert alert, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, string userId, CancellationToken cancellationToken);
}

public sealed class ObservationAlertRepository(AppDbContext db) : IObservationAlertRepository
{
    public Task<List<ObservationAlert>> GetForUserAsync(string userId, CancellationToken cancellationToken) =>
        db.ObservationAlerts
            .AsNoTracking()
            .Include(alert => alert.CelestialObject)
            .Where(alert => alert.UserId == userId)
            .OrderByDescending(alert => alert.Id)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ObservationAlert alert, CancellationToken cancellationToken)
    {
        db.ObservationAlerts.Add(alert);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, string userId, CancellationToken cancellationToken)
    {
        int deleted = await db.ObservationAlerts
            .Where(alert => alert.Id == id && alert.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted > 0;
    }
}
