using AstralObservatory.Models;
using AstralObservatory.Configuration;
using Microsoft.Extensions.Options;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AstralObservatory.Services;

public interface ISmsService
{
    string DeliveryMode { get; }
    Task<SmsDeliveryResult> SendCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken);
}

public sealed record SmsDeliveryResult(string MaskedDestination, string? DevelopmentCode);

public sealed class SmsDeliveryException(string message, Exception? innerException = null) : Exception(message, innerException);

public sealed class DevelopmentSmsService(ILogger<DevelopmentSmsService> logger) : ISmsService
{
    public string DeliveryMode => "development";

    public Task<SmsDeliveryResult> SendCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken)
    {
        string destination = MaskPhoneNumber(phoneNumber);
        logger.LogInformation("Development SMS generated for {Destination}. Code: {Code}", destination, code);
        return Task.FromResult(new SmsDeliveryResult(destination, code));
    }

    internal static string MaskPhoneNumber(string phoneNumber) =>
        phoneNumber.Length <= 4 ? "••••" : $"••••••{phoneNumber[^4..]}";
}

public sealed class TwilioSmsService : ISmsService
{
    private readonly TwilioRestClient _client;
    private readonly PhoneNumber _fromNumber;
    private readonly ILogger<TwilioSmsService> _logger;

    public TwilioSmsService(IOptions<SmsOptions> options, ILogger<TwilioSmsService> logger)
    {
        SmsOptions configuration = options.Value;
        _client = new TwilioRestClient(configuration.AccountSid!, configuration.AuthToken!);
        _fromNumber = new PhoneNumber(configuration.FromNumber!);
        _logger = logger;
    }

    public string DeliveryMode => "twilio";

    public async Task<SmsDeliveryResult> SendCodeAsync(string phoneNumber, string code, CancellationToken cancellationToken)
    {
        string destination = DevelopmentSmsService.MaskPhoneNumber(phoneNumber);
        try
        {
            await MessageResource.CreateAsync(
                to: new PhoneNumber(phoneNumber),
                from: _fromNumber,
                body: $"Your Astral Observatory verification code is {code}",
                client: _client);
            _logger.LogInformation("Verification SMS accepted by Twilio for {Destination}", destination);
            return new SmsDeliveryResult(destination, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Twilio could not deliver a verification SMS to {Destination}", destination);
            throw new SmsDeliveryException("The verification SMS could not be sent", exception);
        }
    }
}

public interface IEmailService
{
    Task SendObservationAlertAsync(ApplicationUser user, CelestialObject item, double value, CancellationToken cancellationToken);
}

public sealed class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendObservationAlertAsync(ApplicationUser user, CelestialObject item, double value, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Development email created for {Recipient}. {ObjectName} {Metric} reached {Value:F1}",
            MaskEmail(user.Email),
            item.Name,
            item.ActivityMetric,
            value);
        return Task.CompletedTask;
    }

    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            return "hidden recipient";
        }

        string[] parts = email.Split('@', 2);
        string local = parts[0].Length <= 2 ? "••" : $"{parts[0][0]}••{parts[0][^1]}";
        return $"{local}@{parts[1]}";
    }
}
