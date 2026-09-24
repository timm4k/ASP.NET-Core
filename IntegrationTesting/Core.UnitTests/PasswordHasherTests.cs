using Application.Core.Users;

namespace Core.UnitTests;

public sealed class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_CreatesVerifiableValueWithoutPlainTextPassword()
    {
        const string password = "SafePass1";

        var hash = _hasher.Hash(password);

        Assert.NotEqual(password, hash);
        Assert.DoesNotContain(password, hash, StringComparison.Ordinal);
        Assert.True(_hasher.Verify(password, hash));
        Assert.False(_hasher.Verify("WrongPass1", hash));
    }

    [Fact]
    public void Hash_UsesRandomSalt()
    {
        var first = _hasher.Hash("SafePass1");
        var second = _hasher.Hash("SafePass1");

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text")]
    [InlineData("pbkdf2-sha256$1$invalid$invalid")]
    public void Verify_WithMalformedHash_ReturnsFalse(string encodedHash)
    {
        Assert.False(_hasher.Verify("SafePass1", encodedHash));
    }
}
