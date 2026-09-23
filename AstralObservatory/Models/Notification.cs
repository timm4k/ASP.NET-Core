namespace AstralObservatory.Models;

public sealed class Notification
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public required string Message { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
