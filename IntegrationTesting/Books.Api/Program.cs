using Books.Api.Books;
using Books.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var connectionString = DatabaseConnectionString.Resolve(
    builder.Configuration["Database:ConnectionString"]);

builder.Services.AddSingleton(new BookDatabaseSettings(connectionString));
builder.Services.AddSingleton<IBookRepository, SqliteBookRepository>();
builder.Services.AddScoped<BookService>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();

await BookDatabaseInitializer.InitializeAsync(connectionString);
app.MapBookEndpoints();

app.Run();

public partial class Program;
