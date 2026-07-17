namespace HBP.Api.HostedServices;

public sealed class EmailDispatchOptions
{
    public int PollIntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 10;
    public int MaxAttempts { get; set; } = 6;
    public int RetentionDays { get; set; } = 90;
}
