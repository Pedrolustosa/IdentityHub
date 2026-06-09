using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentityHub.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestKind = ResolveRequestKind(typeof(TRequest), requestName);

        _logger.LogInformation(
            "Handling {RequestKind} {RequestName}",
            requestKind,
            requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Handled {RequestKind} {RequestName} in {ElapsedMs} ms",
                requestKind,
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Failed {RequestKind} {RequestName} after {ElapsedMs} ms",
                requestKind,
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private static string ResolveRequestKind(Type requestType, string requestName)
    {
        if (requestName.EndsWith("Query", StringComparison.Ordinal))
            return "Query";

        if (requestName.EndsWith("Command", StringComparison.Ordinal))
            return "Command";

        var requestInterface = requestType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));

        if (requestInterface is not null)
        {
            var responseTypeName = requestInterface.GetGenericArguments()[0].Name;
            if (responseTypeName.Contains("Query", StringComparison.Ordinal))
                return "Query";
        }

        return "Request";
    }
}
