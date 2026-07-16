using HBP.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HBP.Api.Infrastructure;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (404, "Resource not found"),
            ValidationException => (400, "Validation failed"),
            ConflictException => (409, "Conflict"),
            _ => (500, "An unexpected error occurred")
        };
        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Title = title, Detail = status == 500 ? null : exception.Message };
        if (exception is ValidationException validation)
            details.Extensions["errors"] = validation.Errors;
        if (exception is MediaInUseException media)
            details.Extensions["references"] = media.References;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details, Exception = exception });
    }
}
