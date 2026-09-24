using Microsoft.Data.Sqlite;

namespace Books.Api.Data;

public static class DatabaseConnectionString
{
    public static string Resolve(string? configuredConnectionString)
    {
        var rawConnectionString = string.IsNullOrWhiteSpace(configuredConnectionString)
            ? "Data Source=books.db"
            : configuredConnectionString;
        var builder = new SqliteConnectionStringBuilder(rawConnectionString);

        if (builder.DataSource != ":memory:" && !Path.IsPathFullyQualified(builder.DataSource))
        {
            var dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "IntegrationTesting");
            Directory.CreateDirectory(dataDirectory);
            builder.DataSource = Path.Combine(dataDirectory, builder.DataSource);
        }

        return builder.ConnectionString;
    }
}
