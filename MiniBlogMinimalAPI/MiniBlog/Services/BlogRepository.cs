using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MiniBlog.Models;

namespace MiniBlog.Services;

public sealed partial class BlogRepository
{
    private const string DefaultDataPath = "Data/blog-data.json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _dataPath;
    private BlogData _data = new();

    public BlogRepository(IWebHostEnvironment environment, IConfiguration configuration)
    {
        string configuredPath = configuration["Blog:DataPath"] ?? DefaultDataPath;
        _dataPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            string? directory = Path.GetDirectoryName(_dataPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_dataPath))
            {
                _data = new BlogData();
                await SaveUnsafeAsync(cancellationToken);
                return;
            }

            await using (FileStream stream = File.OpenRead(_dataPath))
            {
                _data = await JsonSerializer.DeserializeAsync<BlogData>(
                    stream,
                    SerializerOptions,
                    cancellationToken) ?? new BlogData();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string HashAuthorKey(string authorKey)
    {
        byte[] value = Encoding.UTF8.GetBytes(authorKey.Trim());
        return Convert.ToHexString(SHA256.HashData(value));
    }

    private static bool AuthorKeyMatches(string storedHash, string authorKey)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        try
        {
            byte[] stored = Convert.FromHexString(storedHash);
            byte[] supplied = Convert.FromHexString(HashAuthorKey(authorKey));
            return stored.Length == supplied.Length &&
                CryptographicOperations.FixedTimeEquals(stored, supplied);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static CommandResult<T> NotFound<T>(string entityName)
    {
        return CommandResult<T>.Invalid(new Dictionary<string, string[]>
        {
            ["id"] = [$"{entityName} was not found"]
        });
    }

    private async Task SaveUnsafeAsync(CancellationToken cancellationToken)
    {
        string temporaryPath = $"{_dataPath}.tmp";
        try
        {
            await using (FileStream stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, _data, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            if (File.Exists(_dataPath))
            {
                File.Replace(temporaryPath, _dataPath, null);
            }
            else
            {
                File.Move(temporaryPath, _dataPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
