namespace AstralObservatory.Configuration;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public TimeSpan Interval { get; init; }
    public TimeSpan AlertReactivationDelay { get; init; }
    public TimeSpan LongPollingTimeout { get; init; }
    public TimeSpan LongPollingInterval { get; init; }

    public bool IsValid() =>
        Interval > TimeSpan.Zero
        && AlertReactivationDelay > TimeSpan.Zero
        && LongPollingTimeout > TimeSpan.Zero
        && LongPollingInterval > TimeSpan.Zero
        && LongPollingInterval < LongPollingTimeout;
}
