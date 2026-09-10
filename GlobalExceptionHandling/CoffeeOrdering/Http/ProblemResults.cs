namespace CoffeeOrdering.Http;

public static class ProblemResults
{
    public static IResult Problem(
        int status,
        string title,
        string detail,
        string reason,
        string localizedMessage,
        string developerMessage) => Results.Problem(
            statusCode: status,
            title: title,
            detail: detail,
            type: $"/problems/{reason.ToLowerInvariant().Replace('_', '-')}",
            extensions: Extensions(reason, localizedMessage, developerMessage));

    public static IResult Validation(
        Dictionary<string, string[]> errors,
        string reason,
        string localizedMessage,
        string developerMessage) => Results.ValidationProblem(
            errors,
            statusCode: StatusCodes.Status400BadRequest,
            title: "Request validation failed",
            type: $"/problems/{reason.ToLowerInvariant().Replace('_', '-')}",
            extensions: Extensions(reason, localizedMessage, developerMessage));

    private static Dictionary<string, object?> Extensions(
        string reason,
        string localizedMessage,
        string developerMessage) => new()
        {
            ["reason"] = reason,
            ["localized_message"] = localizedMessage,
            ["developer_message"] = developerMessage
        };
}
