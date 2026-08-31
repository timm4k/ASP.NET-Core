using System.Text;

namespace DependencyInjectionLab.Logging;

internal interface ILogger
{
    void Log(string message);
}

internal sealed class FileLogger : ILogger
{
    private readonly string _filePath;

    public FileLogger(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Log file path cannot be empty", nameof(filePath));
        }

        _filePath = filePath;
    }

    public void Log(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        File.AppendAllText(_filePath, $"{message}{Environment.NewLine}", Encoding.UTF8);
    }
}
