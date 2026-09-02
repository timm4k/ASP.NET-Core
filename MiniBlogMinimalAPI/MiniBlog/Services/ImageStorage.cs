using MiniBlog.Models;

namespace MiniBlog.Services;

public sealed class ImageStorage(IWebHostEnvironment environment)
{
    public const int MaximumFileSizeMegabytes = 8;
    public const long MaximumFileSize = MaximumFileSizeMegabytes * 1024L * 1024L;

    private readonly string _uploadDirectory = Path.Combine(environment.WebRootPath, "uploads");

    public async Task<CommandResult<string>> SaveAsync(
        IFormFile? image,
        CancellationToken cancellationToken = default)
    {
        if (image is null || image.Length == 0)
        {
            return Invalid("Choose an image file");
        }

        if (image.Length > MaximumFileSize)
        {
            return Invalid($"Image must not exceed {MaximumFileSizeMegabytes} MB");
        }

        await using MemoryStream buffer = new((int)image.Length);
        await image.CopyToAsync(buffer, cancellationToken);
        string? extension = DetectExtension(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        if (extension is null)
        {
            return Invalid("Only valid JPG, PNG and WebP images are allowed");
        }

        Directory.CreateDirectory(_uploadDirectory);
        string fileName = $"{Guid.NewGuid():N}{extension}";
        string filePath = Path.Combine(_uploadDirectory, fileName);
        buffer.Position = 0;

        await using FileStream destination = new(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        await buffer.CopyToAsync(destination, cancellationToken);

        return CommandResult<string>.Success($"/uploads/{fileName}");
    }

    private static string? DetectExtension(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
        {
            return ".jpg";
        }
        if (content.Length >= 8 && content[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
        {
            return ".png";
        }
        if (content.Length >= 12 &&
            content[..4].SequenceEqual("RIFF"u8) &&
            content.Slice(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }
        return null;
    }

    private static CommandResult<string> Invalid(string message)
    {
        return CommandResult<string>.Invalid(new Dictionary<string, string[]>
        {
            ["image"] = [message]
        });
    }
}
