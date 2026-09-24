using System.Net;
using System.Net.Http.Json;
using Books.Api.Books;

namespace Books.Api.IntegrationTests;

public sealed class BookApiTests(BooksApiFactory factory) : IClassFixture<BooksApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBooks_ReturnsOkWithNonEmptyList()
    {
        var response = await _client.GetAsync("/api/books");
        var books = await response.Content.ReadFromJsonAsync<List<BookResponse>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(books);
        Assert.NotEmpty(books);
    }

    [Fact]
    public async Task GetBook_WithExistingId_ReturnsOk()
    {
        var created = await CreateBookAsync("Dune", "Frank Herbert");

        var response = await _client.GetAsync($"/api/books/{created.Id}");
        var book = await response.Content.ReadFromJsonAsync<BookResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(book);
        Assert.Equal("Dune", book.Title);
    }

    [Fact]
    public async Task GetBook_WithMissingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/books/{int.MaxValue}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateBook_WithValidFields_ReturnsCreatedBook()
    {
        var response = await _client.PostAsJsonAsync("/api/books", new SaveBookRequest("Kindred", "Octavia E. Butler"));
        var book = await response.Content.ReadFromJsonAsync<BookResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(book);
        Assert.True(book.Id > 0);
        Assert.Equal("Kindred", book.Title);
        Assert.Equal("Octavia E. Butler", book.Author);
        Assert.Equal($"/api/books/{book.Id}", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("", "Author")]
    [InlineData("Title", "")]
    [InlineData("   ", "   ")]
    public async Task CreateBook_WithBlankRequiredField_ReturnsBadRequest(string title, string author)
    {
        var response = await _client.PostAsJsonAsync("/api/books", new SaveBookRequest(title, author));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBook_WithExistingId_ReturnsNoContentAndPersistsChanges()
    {
        var created = await CreateBookAsync("Original", "Original Author");

        var response = await _client.PutAsJsonAsync(
            $"/api/books/{created.Id}",
            new SaveBookRequest("Updated", "Updated Author"));
        var stored = await _client.GetFromJsonAsync<BookResponse>($"/api/books/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.NotNull(stored);
        Assert.Equal("Updated", stored.Title);
        Assert.Equal("Updated Author", stored.Author);
    }

    [Fact]
    public async Task UpdateBook_WithMissingId_ReturnsNotFound()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/books/{int.MaxValue}",
            new SaveBookRequest("Missing", "Missing Author"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBook_RemovesBook()
    {
        var created = await CreateBookAsync("Temporary", "Temporary Author");

        var deleteResponse = await _client.DeleteAsync($"/api/books/{created.Id}");
        var getResponse = await _client.GetAsync($"/api/books/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<BookResponse> CreateBookAsync(string title, string author)
    {
        var response = await _client.PostAsJsonAsync("/api/books", new SaveBookRequest(title, author));
        response.EnsureSuccessStatusCode();
        var book = await response.Content.ReadFromJsonAsync<BookResponse>();
        return Assert.IsType<BookResponse>(book);
    }
}
