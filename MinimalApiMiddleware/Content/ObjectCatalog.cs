namespace MinimalApiMiddleware.Content;

internal sealed record DisplayObject(string Name, string Category, string Description);

internal static class ObjectCatalog
{
    public static IReadOnlyList<DisplayObject> All { get; } =
    [
        new("Request", "HTTP", "Carries method, path, headers, query, and body"),
        new("Response", "HTTP", "Returns a status, headers, and content"),
        new("Middleware", "Pipeline", "Processes a request and can call the next component"),
        new("Header", "Metadata", "Describes request or response details"),
        new("Query string", "Input", "Passes small values through the request URL"),
        new("Status code", "Protocol", "Communicates the result of request processing"),
        new("JSON", "Format", "Transfers structured data between server and client"),
        new("HTML", "Interface", "Defines the document displayed by the browser"),
        new("CSS", "Interface", "Controls layout, color, and responsive behavior"),
        new("JavaScript", "Interface", "Connects browser interactions to HTTP endpoints")
    ];
}
