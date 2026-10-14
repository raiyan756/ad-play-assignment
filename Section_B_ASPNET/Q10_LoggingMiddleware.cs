using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

// Q10: Body logging is disabled by default. Safe capture is explained in the DOCX.
public sealed class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers["X-Correlation-ID"].ToString();
        var correlationId = supplied.Length is > 0 and <= 64 &&
            supplied.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? supplied : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            return Task.CompletedTask;
        });
        using var scope = logger.BeginScope(new Dictionary<string, object>
        { ["CorrelationId"] = correlationId });
        var watch = Stopwatch.StartNew();
        bool failed = false, cancelled = false;
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            cancelled = true;
            logger.LogInformation("Request {CorrelationId} cancelled by the client", correlationId);
            throw;
        }
        catch (Exception ex)
        {
            failed = true;
            // Exception messages may contain secrets or SQL parameter values.
            logger.LogError("Request {CorrelationId}: unhandled {ExceptionType} at {StackTrace}",
                correlationId, ex.GetType().FullName, ex.StackTrace);
            throw;
        }
        finally
        {
            var route = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "unmatched";
            logger.LogInformation(
                "Request {CorrelationId}: HTTP {Method} {Route} status {Status} in {ElapsedMs} ms cancelled {Cancelled}",
                correlationId, context.Request.Method, route, failed ? 500 : context.Response.StatusCode,
                watch.Elapsed.TotalMilliseconds, cancelled);
        }
    }
}

// Registration in an existing application:
// builder.Services.AddProblemDetails();
// Disable the outer handler's raw exception logs; this middleware logs safe details.
// builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
// app.UseExceptionHandler();            // Outer handler produces a safe error response.
// app.UseRouting();
// app.UseMiddleware<LoggingMiddleware>();
// Map endpoints after the middleware.
