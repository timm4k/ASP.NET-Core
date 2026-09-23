using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace AstralObservatory.Infrastructure;

public sealed class IdentityLookupProtectorKeyRing : ILookupProtectorKeyRing
{
    private const string KeyId = "identity-v1";
    private readonly string _key;

    public IdentityLookupProtectorKeyRing(IDataProtectionProvider provider)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AstralObservatory");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "identity-lookup.key");
        IDataProtector protector = provider.CreateProtector("AstralObservatory.Identity.LookupKey");

        if (File.Exists(path))
        {
            _key = protector.Unprotect(File.ReadAllText(path));
        }
        else
        {
            _key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            File.WriteAllText(path, protector.Protect(_key));
        }
    }

    public string CurrentKeyId => KeyId;
    public string this[string keyId] => keyId == KeyId ? _key : throw new KeyNotFoundException("Identity protection key was not found");
    public IEnumerable<string> GetAllKeyIds() => [KeyId];
}

public sealed class IdentityLookupProtector(ILookupProtectorKeyRing keyRing) : ILookupProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string? Protect(string keyId, string? data)
    {
        if (data is null)
        {
            return null;
        }

        byte[] key = Convert.FromBase64String(keyRing[keyId]);
        byte[] plaintext = Encoding.UTF8.GetBytes(data);
        byte[] nonce = HMACSHA256.HashData(key, plaintext)[..NonceSize];
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];
        using AesGcm aes = new(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        byte[] protectedData = new byte[NonceSize + ciphertext.Length + TagSize];
        nonce.CopyTo(protectedData, 0);
        ciphertext.CopyTo(protectedData, NonceSize);
        tag.CopyTo(protectedData, NonceSize + ciphertext.Length);
        return Convert.ToBase64String(protectedData);
    }

    public string? Unprotect(string keyId, string? data)
    {
        if (data is null)
        {
            return null;
        }

        byte[] key = Convert.FromBase64String(keyRing[keyId]);
        byte[] protectedData = Convert.FromBase64String(data);
        if (protectedData.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Protected Identity data is invalid");
        }

        ReadOnlySpan<byte> nonce = protectedData.AsSpan(0, NonceSize);
        ReadOnlySpan<byte> ciphertext = protectedData.AsSpan(NonceSize, protectedData.Length - NonceSize - TagSize);
        ReadOnlySpan<byte> tag = protectedData.AsSpan(protectedData.Length - TagSize, TagSize);
        byte[] plaintext = new byte[ciphertext.Length];
        using AesGcm aes = new(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
