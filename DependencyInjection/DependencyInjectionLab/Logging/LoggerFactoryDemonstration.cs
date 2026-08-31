using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjectionLab.Logging;

internal static class LoggerFactoryDemonstration
{
    public static void Run()
    {
        Console.WriteLine("2.3 Factory logger registration");

        string filePath = ReadFilePath();

        ServiceCollection services = new();
        services.AddTransient<ILogger>(_ => new FileLogger(filePath));

        using ServiceProvider provider = services.BuildServiceProvider();
        ILogger logger = provider.GetRequiredService<ILogger>();
        logger.Log("Dependency injection demonstration completed");
        Console.WriteLine($"Message written to {filePath}");
    }

    private static string ReadFilePath()
    {
        while (true)
        {
            Console.Write("Log file path: ");
            string? input = Console.ReadLine();

            if (TryResolveFilePath(input, out string filePath))
            {
                return filePath;
            }

            Console.WriteLine("Enter a valid path to a file in an existing directory");
        }
    }

    private static bool TryResolveFilePath(string? input, out string filePath)
    {
        filePath = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        try
        {
            filePath = Path.GetFullPath(input.Trim().Trim('"'));
            string? directoryPath = Path.GetDirectoryName(filePath);
            return !string.IsNullOrWhiteSpace(Path.GetFileName(filePath))
                && directoryPath is not null
                && Directory.Exists(directoryPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}
