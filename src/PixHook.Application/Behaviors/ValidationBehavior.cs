using System.Collections.Concurrent;
using System.Reflection;
using FluentValidation;
using MediatR;
using PixHook.Application.Common;

namespace PixHook.Application.Behaviors;

/// <summary>
/// Executa os validators do FluentValidation antes do handler. Em caso de falha,
/// devolve um <see cref="Result{T}"/> com <see cref="ErrorType.Validation"/> (sem lançar exceção).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> FailureFactories = new();
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count == 0) return await next();

        var details = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        var error = Error.Validation("validation_failed", "Um ou mais campos são inválidos.", details);

        var factory = FailureFactories.GetOrAdd(typeof(TResponse), t =>
            t.GetMethod(nameof(Result<object>.Failure), BindingFlags.Public | BindingFlags.Static, new[] { typeof(Error) })
            ?? throw new InvalidOperationException($"{t.Name} não possui Failure(Error)."));

        return (TResponse)factory.Invoke(null, new object[] { error })!;
    }
}
