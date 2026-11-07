namespace PixHook.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
