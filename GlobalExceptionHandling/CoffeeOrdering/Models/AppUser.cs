namespace CoffeeOrdering.Models;

public sealed class AppUser
{
    public AppUser(Guid id, string username)
    {
        Id = id;
        Username = username;
    }

    public Guid Id { get; }
    public string Username { get; }
    public string PasswordHash { get; set; } = string.Empty;
}
