namespace AstralObservatory.Models;

public sealed class Observation
{
    public int Id { get; set; }
    public int CelestialObjectId { get; set; }
    public CelestialObject CelestialObject { get; set; } = null!;
    public double ActivityLevel { get; set; }
    public required string ObservationType { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
}
