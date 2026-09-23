namespace AstralObservatory.Models;

public sealed class CelestialObject
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public CelestialObjectType Type { get; set; }
    public required string Description { get; set; }
    public required string ActivityMetric { get; set; }
    public double CurrentActivity { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public List<Observation> Observations { get; set; } = [];
    public List<ObservationAlert> Alerts { get; set; } = [];
}

public enum CelestialObjectType
{
    Star,
    Moon,
    Comet,
    Planet,
    Galaxy
}
