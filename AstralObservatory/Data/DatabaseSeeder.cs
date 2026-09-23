using AstralObservatory.Models;
using Microsoft.EntityFrameworkCore;

namespace AstralObservatory.Data;

internal static class DatabaseSeeder
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.CelestialObjects.AnyAsync())
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        db.CelestialObjects.AddRange(
            CreateObject("Sun", CelestialObjectType.Star, "Solar Activity", 73,
                "The star at the center of our system and the observatory's primary activity source", now),
            CreateObject("Moon", CelestialObjectType.Moon, "Tidal Activity", 42,
                "Earth's natural satellite, observed for tidal and surface activity", now),
            CreateObject("Halley's Comet", CelestialObjectType.Comet, "Visibility", 56,
                "A short-period comet returning to the inner Solar System roughly every 76 years", now),
            CreateObject("Mars", CelestialObjectType.Planet, "Atmospheric Activity", 31,
                "A rocky planet monitored for dust storms and atmospheric variation", now),
            CreateObject("Saturn", CelestialObjectType.Planet, "Magnetic Activity", 64,
                "A gas giant with a complex ring system and a dynamic magnetosphere", now),
            CreateObject("Andromeda", CelestialObjectType.Galaxy, "Luminosity Index", 48,
                "The nearest large galaxy to the Milky Way and a long-term observation target", now));

        await db.SaveChangesAsync();
    }

    private static CelestialObject CreateObject(
        string name,
        CelestialObjectType type,
        string metric,
        double activity,
        string description,
        DateTimeOffset observedAt)
    {
        CelestialObject item = new()
        {
            Name = name,
            Type = type,
            ActivityMetric = metric,
            CurrentActivity = activity,
            Description = description,
            LastUpdated = observedAt
        };

        item.Observations.Add(new Observation
        {
            ActivityLevel = activity,
            ObservationType = metric,
            ObservedAt = observedAt
        });

        return item;
    }
}
