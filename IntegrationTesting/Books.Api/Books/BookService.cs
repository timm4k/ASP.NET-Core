namespace Books.Api.Books;

public sealed class BookService(IBookRepository repository)
{
    public async Task<IReadOnlyList<BookResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var books = await repository.GetAllAsync(cancellationToken);
        return books.Select(ToResponse).ToArray();
    }

    public async Task<BookResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            return null;
        }

        var book = await repository.GetByIdAsync(id, cancellationToken);
        return book is null ? null : ToResponse(book);
    }

    public async Task<BookResponse> CreateAsync(SaveBookRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        var created = await repository.CreateAsync(
            new Book { Title = request.Title.Trim(), Author = request.Author.Trim() },
            cancellationToken);
        return ToResponse(created);
    }

    public async Task<bool> UpdateAsync(int id, SaveBookRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);
        if (id <= 0)
        {
            return false;
        }

        return await repository.UpdateAsync(
            new Book { Id = id, Title = request.Title.Trim(), Author = request.Author.Trim() },
            cancellationToken);
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        id <= 0 ? Task.FromResult(false) : repository.DeleteAsync(id, cancellationToken);

    private static BookResponse ToResponse(Book book) => new(book.Id, book.Title, book.Author);

    private static void Validate(SaveBookRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200)
        {
            errors[nameof(request.Title)] = ["Title is required and must not exceed 200 characters"];
        }

        if (string.IsNullOrWhiteSpace(request.Author) || request.Author.Trim().Length > 120)
        {
            errors[nameof(request.Author)] = ["Author is required and must not exceed 120 characters"];
        }

        if (errors.Count > 0)
        {
            throw new BookValidationException(errors);
        }
    }
}
