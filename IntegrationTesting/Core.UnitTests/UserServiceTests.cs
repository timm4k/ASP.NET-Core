using Application.Core.Users;
using Moq;

namespace Core.UnitTests;

public sealed class UserServiceTests
{
    private readonly Mock<IUserRepository> _repository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();

    [Fact]
    public async Task CreateAsync_WithValidInput_HashesPasswordAndReturnsSafeResponse()
    {
        const string password = "SafePass1";
        const string hash = "secure-hash";
        User? storedUser = null;
        _passwordHasher.Setup(hasher => hasher.Hash(password)).Returns(hash);
        _repository
            .Setup(repository => repository.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => storedUser = user)
            .ReturnsAsync((User user, CancellationToken _) =>
            {
                user.Id = 17;
                return user;
            });
        var service = CreateService();

        var result = await service.CreateAsync(new CreateUserRequest("  Tanya  ", "TANYA@example.com", password));

        Assert.NotNull(storedUser);
        Assert.Equal(hash, storedUser.PasswordHash);
        Assert.Equal("Tanya", result.Name);
        Assert.Equal("tanya@example.com", result.Email);
        Assert.Equal(17, result.Id);
        Assert.DoesNotContain(password, result.ToString(), StringComparison.Ordinal);
        _passwordHasher.Verify(hasher => hasher.Hash(password), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidInput_DoesNotCallDependencies()
    {
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InputValidationException>(() =>
            service.CreateAsync(new CreateUserRequest("", "invalid", "weak")));

        Assert.Contains("name", exception.Errors.Keys);
        Assert.Contains("email", exception.Errors.Keys);
        Assert.Contains("password", exception.Errors.Keys);
        _passwordHasher.VerifyNoOtherCalls();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAsync_MapsUsersWithoutPasswordHashes()
    {
        _repository
            .Setup(repository => repository.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new User { Id = 1, Name = "Ada", Email = "ada@example.com", PasswordHash = "hidden" },
                new User { Id = 2, Name = "Linus", Email = "linus@example.com", PasswordHash = "hidden" }
            ]);
        var service = CreateService();

        var result = await service.GetAllAsync();

        Assert.Collection(
            result,
            user => Assert.Equal("ada@example.com", user.Email),
            user => Assert.Equal("linus@example.com", user.Email));
        Assert.All(result, user => Assert.DoesNotContain("hidden", user.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsSafeResponse()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 5, Name = "Grace", Email = "grace@example.com", PasswordHash = "hidden" });
        var service = CreateService();

        var result = await service.GetByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal("Grace", result.Name);
        Assert.DoesNotContain("hidden", result.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateAsync_WithNewPassword_HashesAndPersistsChanges()
    {
        var user = new User { Id = 4, Name = "Old", Email = "old@example.com", PasswordHash = "old-hash" };
        _repository.Setup(repository => repository.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _repository.Setup(repository => repository.UpdateAsync(user, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _passwordHasher.Setup(hasher => hasher.Hash("NewPass2")).Returns("new-hash");
        var service = CreateService();

        var updated = await service.UpdateAsync(4, new UpdateUserRequest("New Name", "NEW@example.com", "NewPass2"));

        Assert.True(updated);
        Assert.Equal("New Name", user.Name);
        Assert.Equal("new@example.com", user.Email);
        Assert.Equal("new-hash", user.PasswordHash);
        _repository.Verify(repository => repository.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        _repository.Setup(repository => repository.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var service = CreateService();

        var updated = await service.UpdateAsync(99, new UpdateUserRequest("Valid Name", "valid@example.com", null));

        Assert.False(updated);
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        _repository.Setup(repository => repository.DeleteAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = CreateService();

        var deleted = await service.DeleteAsync(8);

        Assert.True(deleted);
        _repository.Verify(repository => repository.DeleteAsync(8, It.IsAny<CancellationToken>()), Times.Once);
    }

    private UserService CreateService() => new(_repository.Object, _passwordHasher.Object);
}
