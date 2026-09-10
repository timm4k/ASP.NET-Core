using System.Collections.Concurrent;
using CoffeeOrdering.Models;
using Microsoft.AspNetCore.Identity;

namespace CoffeeOrdering.Services;

public sealed class UserStore(IPasswordHasher<AppUser> passwordHasher)
{
    private readonly ConcurrentDictionary<string, AppUser> _usersByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, AppUser> _usersById = [];

    public bool TryCreate(string username, string password, out AppUser user)
    {
        user = new AppUser(Guid.NewGuid(), username.Trim());
        user.PasswordHash = passwordHasher.HashPassword(user, password);

        if (!_usersByName.TryAdd(user.Username, user))
        {
            return false;
        }

        _usersById.TryAdd(user.Id, user);
        return true;
    }

    public AppUser? ValidateCredentials(string username, string password)
    {
        if (!_usersByName.TryGetValue(username.Trim(), out AppUser? user))
        {
            return null;
        }

        PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, password);
        }

        return user;
    }

    public AppUser? FindById(Guid id) => _usersById.GetValueOrDefault(id);
}
