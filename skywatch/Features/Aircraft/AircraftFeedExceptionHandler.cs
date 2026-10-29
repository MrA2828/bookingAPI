using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SkyWatch.Features.Aircraft.Sources;

namespace SkyWatch.Features.Aircraft;

// Turns "no live data source reachable" into a 503 instead of a generic 500.
public class AircraftFeedExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not AircraftFeedUnavailableException) return false;

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Live aircraft data is temporarily unavailable",
                Detail = exception.Message
            }
        });
    }
}
