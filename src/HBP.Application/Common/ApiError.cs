namespace HBP.Application.Common;

public sealed record ApiError(string Code, string Message, IReadOnlyDictionary<string, string[]>? Errors = null);
