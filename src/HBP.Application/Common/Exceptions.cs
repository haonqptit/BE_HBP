namespace HBP.Application.Common;

public class NotFoundException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);

public class ValidationException(
    string message,
    IReadOnlyDictionary<string, string[]>? errors = null) : Exception(message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } =
        errors ?? new Dictionary<string, string[]>();
}

public sealed class MediaInUseException(IReadOnlyList<string> references)
    : ConflictException("Media is currently in use and cannot be deleted.")
{
    public IReadOnlyList<string> References { get; } = references;
}
