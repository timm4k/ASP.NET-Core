using Microsoft.Data.Sqlite;

namespace Books.Api.Data;

public static class BookDatabaseInitializer
{
    private static readonly (string Title, string Author)[] SeedBooks =
    [
        ("A Killer's Mind", "Mike Omer"),
        ("In the Darkness", "Mike Omer"),
        ("Thicker Than Blood", "Mike Omer"),
        ("The Murder of Roger Ackroyd", "Agatha Christie"),
        ("The Girl with the Dragon Tattoo", "Stieg Larsson"),
        ("The Snowman", "Jo Nesbø"),
        ("The Silent Patient", "Alex Michaelides")
    ];

    public static async Task InitializeAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = """
            CREATE TABLE IF NOT EXISTS Books (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Author TEXT NOT NULL
            );
            """;
        await schemaCommand.ExecuteNonQueryAsync(cancellationToken);

        foreach (var book in SeedBooks)
        {
            await using var seedCommand = connection.CreateCommand();
            seedCommand.CommandText = """
                INSERT INTO Books (Title, Author)
                SELECT $title, $author
                WHERE NOT EXISTS (
                    SELECT 1 FROM Books WHERE Title = $title AND Author = $author
                );
                """;
            seedCommand.Parameters.AddWithValue("$title", book.Title);
            seedCommand.Parameters.AddWithValue("$author", book.Author);
            await seedCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
