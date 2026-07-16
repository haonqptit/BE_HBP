namespace HBP.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
