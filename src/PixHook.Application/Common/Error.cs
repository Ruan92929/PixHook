namespace PixHook.Application.Common;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Unprocessable = 3
}

public sealed record Error(string Code, string Message, ErrorType Type, IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? details = null) =>
        new(code, message, ErrorType.Validation, details);
    public static Error Unprocessable(string code, string message, IReadOnlyDictionary<string, string[]>? details = null) =>
        new(code, message, ErrorType.Unprocessable, details);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}
