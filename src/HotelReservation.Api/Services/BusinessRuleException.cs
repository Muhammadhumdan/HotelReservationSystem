using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HotelReservation.Api.Services;

/// <summary>Thrown when the user asks for something the hotel rules do not allow (e.g. room already booked).</summary>
public class BusinessRuleException(string message, int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

/// <summary>
/// One central place that turns a BusinessRuleException into a neat JSON error (ProblemDetails)
/// so controllers do not need try/catch everywhere.
/// </summary>
public class BusinessRuleExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex) return false;   // not ours -> let default handler run

        httpContext.Response.StatusCode = ex.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = ex.StatusCode,
            Title = "Request could not be completed",
            Detail = ex.Message
        }, cancellationToken);
        return true;
    }
}
