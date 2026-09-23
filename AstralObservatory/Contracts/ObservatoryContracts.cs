namespace AstralObservatory.Contracts;

public sealed record CreateAlertRequest(int CelestialObjectId, double TargetValue);

public sealed record PushSubscriptionRequest(string Endpoint, string P256dh, string Auth);

public sealed record PushMessageRequest(string Title, string Message);

public sealed record TestNotificationRequest(string Message);
