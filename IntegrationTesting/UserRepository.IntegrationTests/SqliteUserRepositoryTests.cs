using Application.Core.Users;
using Microsoft.Data.Sqlite;
using UserData.Sqlite;

namespace UserRepository.IntegrationTests;

public sealed class SqliteUserRepositoryTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"users-{Guid.NewGuid():N}.db");
    private SqliteUserRepository _repository = null!;

    public async Task InitializeAsync()
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false
        }.ConnectionString;
        await new UserDatabaseInitializer(connectionString).InitializeAsync();
        _repository = new SqliteUserRepository(connectionString);
    }

    public Task DisposeAsync()
    {
        File.Delete(_databasePath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateAsync_InsertsUserAndReturnsGeneratedId()
    {
        var user = NewUser("Tanya", "tanya@example.com");

        var created = await _repository.CreateAsync(user);
        var stored = await _repository.GetByIdAsync(created.Id);

        Assert.True(created.Id > 0);
        Assert.NotNull(stored);
        Assert.Equal("Tanya", stored.Name);
        Assert.Equal("hash-value", stored.PasswordHash);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsersInIdentifierOrder()
    {
        await _repository.CreateAsync(NewUser("Ada", "ada@example.com"));
        await _repository.CreateAsync(NewUser("Grace", "grace@example.com"));

        var users = await _repository.GetAllAsync();

        Assert.Collection(
            users,
            user => Assert.Equal("Ada", user.Name),
            user => Assert.Equal("Grace", user.Name));
    }

    [Fact]
    public async Task UpdateAsync_ChangesPersistedValues()
    {
        var user = await _repository.CreateAsync(NewUser("Before", "before@example.com"));
        user.Name = "After";
        user.Email = "after@example.com";
        user.PasswordHash = "new-hash";

        var updated = await _repository.UpdateAsync(user);
        var stored = await _repository.GetByIdAsync(user.Id);

        Assert.True(updated);
        Assert.NotNull(stored);
        Assert.Equal("After", stored.Name);
        Assert.Equal("after@example.com", stored.Email);
        Assert.Equal("new-hash", stored.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        var missingUser = NewUser("Missing", "missing@example.com");
        missingUser.Id = int.MaxValue;

        var updated = await _repository.UpdateAsync(missingUser);

        Assert.False(updated);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        var user = await _repository.CreateAsync(NewUser("Delete Me", "delete@example.com"));

        var deleted = await _repository.DeleteAsync(user.Id);
        var stored = await _repository.GetByIdAsync(user.Id);

        Assert.True(deleted);
        Assert.Null(stored);
    }

    private static User NewUser(string name, string email) => new()
    {
        Name = name,
        Email = email,
        PasswordHash = "hash-value"
    };
}
