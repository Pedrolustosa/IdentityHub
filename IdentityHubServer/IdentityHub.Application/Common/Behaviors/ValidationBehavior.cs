using FluentValidation;
using IdentityHub.Application.Common.Errors;
using IdentityHub.Application.Common.Results;
using MediatR;

namespace IdentityHub.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errorMessage = string.Join(" | ", failures
            .Select(f => f.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct(StringComparer.Ordinal));

        var validationError = Error.Create("Validation.Failed", errorMessage);

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(validationError);

        var responseType = typeof(TResponse);
        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = responseType.GetMethod(
                nameof(Result<object>.Failure),
                [typeof(Error)]);

            if (failureMethod is not null)
            {
                var response = failureMethod.Invoke(null, [validationError]);
                if (response is not null)
                    return (TResponse)response;
            }
        }

        throw new ValidationException(failures);
    }
}
