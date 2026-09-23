namespace AstralObservatory.Models;

public sealed class ObservationAlert
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int CelestialObjectId { get; set; }
    public CelestialObject CelestialObject { get; set; } = null!;
    public double TargetValue { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? TriggeredAt { get; set; }
    public DateTimeOffset? ReactivateAt { get; set; }
}
