namespace AstralObservatory.Models;

public sealed class PushSubscription
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public required string Endpoint { get; set; }
    public required string P256dh { get; set; }
    public required string Auth { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
