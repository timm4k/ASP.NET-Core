using AstralObservatory.Contracts;
using AstralObservatory.Data;
using AstralObservatory.Models;
using AstralObservatory.Repositories;
using AstralObservatory.Services;

namespace AstralObservatory.Endpoints;

public static class ObservatoryEndpoints
{
    public static void MapObservatoryEndpoints(this WebApplication app)
    {
        RouteGroupBuilder objects = app.MapGroup("/api/objects").RequireAuthorization("AccessToken");
        objects.MapGet("/", GetObjectsAsync);
        objects.MapGet("/{id:int}", GetObjectAsync);
        objects.MapGet("/{id:int}/observations", GetObservationsAsync);

        RouteGroupBuilder alerts = app.MapGroup("/api/alerts").RequireAuthorization("AccessToken");
        alerts.MapGet("/", GetAlertsAsync);
        alerts.MapPost("/", CreateAlertAsync);
        alerts.MapDelete("/{id:int}", DeleteAlertAsync);

        app.MapPost("/api/monitor/run", RunMonitorAsync).RequireAuthorization("AccessToken");
    }

    private static async Task<IResult> GetObjectsAsync(ICelestialObjectRepository objects, CancellationToken cancellationToken)
    {
        return Results.Ok(await objects.GetAllAsync(cancellationToken));
    }

    private static async Task<IResult> GetObjectAsync(int id, ICelestialObjectRepository objects, CancellationToken cancellationToken)
    {
        CelestialObject? item = await objects.GetAsync(id, cancellationToken);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> GetObservationsAsync(int id, IObservationRepository observations, CancellationToken cancellationToken)
    {
        List<Observation> history = await observations.GetHistoryAsync(id, 20, cancellationToken);
        return Results.Ok(history);
    }

    private static async Task<IResult> GetAlertsAsync(
        ClaimsPrincipal principal,
        IObservationAlertRepository alerts,
        CancellationToken cancellationToken)
    {
        string? userId = principal.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        List<ObservationAlert> userAlerts = await alerts.GetForUserAsync(userId, cancellationToken);
        return Results.Ok(userAlerts.Select(alert => new
        {
            alert.Id,
            alert.CelestialObjectId,
            ObjectName = alert.CelestialObject.Name,
            Metric = alert.CelestialObject.ActivityMetric,
            alert.TargetValue,
            alert.IsActive,
            alert.CreatedAt,
            alert.TriggeredAt,
            alert.ReactivateAt
        }));
    }

    private static async Task<IResult> CreateAlertAsync(
        CreateAlertRequest request,
        ClaimsPrincipal principal,
        ICelestialObjectRepository objects,
        IObservationAlertRepository alerts,
        CancellationToken cancellationToken)
    {
        string? userId = principal.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        if (request.TargetValue is < 0 or > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["targetValue"] = ["Target value must be between 0 and 100"] });
        }

        if (await objects.GetAsync(request.CelestialObjectId, cancellationToken) is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["celestialObjectId"] = ["Select an existing celestial object"] });
        }

        ObservationAlert alert = new()
        {
            UserId = userId,
            CelestialObjectId = request.CelestialObjectId,
            TargetValue = request.TargetValue,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await alerts.AddAsync(alert, cancellationToken);
        return Results.Created($"/api/alerts/{alert.Id}", new { alert.Id });
    }

    private static async Task<IResult> DeleteAlertAsync(
        int id,
        ClaimsPrincipal principal,
        IObservationAlertRepository alerts,
        CancellationToken cancellationToken)
    {
        string? userId = principal.GetUserId();
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        return await alerts.DeleteAsync(id, userId, cancellationToken) ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> RunMonitorAsync(
        IAstralMonitor monitor,
        CancellationToken cancellationToken) => Results.Ok(await monitor.RunAsync(cancellationToken));
}
