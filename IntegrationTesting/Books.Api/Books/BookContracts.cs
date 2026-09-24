namespace Books.Api.Books;

public sealed record SaveBookRequest(string Title, string Author);

public sealed record BookResponse(int Id, string Title, string Author);
