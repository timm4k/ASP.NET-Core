namespace Application.Core.Users;

public sealed class UserService(IUserRepository repository, IPasswordHasher passwordHasher)
{
    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await repository.GetAllAsync(cancellationToken);
        return users.Select(ToResponse).ToArray();
    }

    public async Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        var user = await repository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        UserInputValidator.ValidateCreate(request);

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        var created = await repository.CreateAsync(user, cancellationToken);
        return ToResponse(created);
    }

    public async Task<bool> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        UserInputValidator.ValidateUpdate(request);

        var user = await repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.Name = request.Name.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        if (request.Password is not null)
        {
            user.PasswordHash = passwordHasher.Hash(request.Password);
        }

        return await repository.UpdateAsync(user, cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        ValidateId(id);
        return repository.DeleteAsync(id, cancellationToken);
    }

    private static UserResponse ToResponse(User user) => new(user.Id, user.Name, user.Email);

    private static void ValidateId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "User identifier must be positive");
        }
    }
}
