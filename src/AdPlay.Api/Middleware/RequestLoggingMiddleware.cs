using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace AdPlay.Api.Middleware;

public class RequestLoggingMiddleware
{
    private const string HeaderName = "X-Correlation-ID";
    private const int MaxLoggedBytes = 4096;                  // how much body text goes in the log
    private const int MaxBufferedRequestBytes = 64 * 1024;    // bigger request bodies are never touched
    private const int SlowMs = 1000;

    // bodies of these endpoints are never logged (card data, credentials)
    private static readonly string[] NoBodyPaths = { "/api/payments", "/api/auth", "/api/login" };
    private static readonly Regex SafeId = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.Request.Path.StartsWithSegments("/openapi"))
        {
            await _next(ctx);
            return;
        }

        var correlationId = GetCorrelationId(ctx);
        ctx.Items["CorrelationId"] = correlationId;
        ctx.Response.OnStarting(() =>
        {
            ctx.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using var scope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        var logBodies = !NoBodyPaths.Any(p => ctx.Request.Path.StartsWithSegments(p));
        var requestBody = logBodies ? await ReadRequestBodyAsync(ctx.Request, ctx.RequestAborted) : null;

        // response goes to the client as normal, we only keep a small copy of the first bytes
        var originalBody = ctx.Response.Body;
        CaptureStream? capture = null;
        if (logBodies)
        {
            capture = new CaptureStream(originalBody, MaxLoggedBytes);
            ctx.Response.Body = capture;
        }

        var sw = Stopwatch.StartNew();
        Exception? error = null;
        var cancelled = false;

        try
        {
            await _next(ctx);
        }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested)
        {
            cancelled = true;   // client left, not a server error
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            ctx.Response.Body = originalBody;
        }

        sw.Stop();

        if (error != null)
            await WriteErrorAsync(ctx, correlationId);

        var status = cancelled ? 499 : ctx.Response.StatusCode;

        string? responseBody = null;
        if (capture != null && error == null && IsJson(ctx.Response.ContentType))
        {
            responseBody = BodyRedactor.Redact(capture.GetText());
            if (capture.Truncated) responseBody += "...[truncated]";
        }

        var level = error != null || status >= 500 ? LogLevel.Error
            : sw.ElapsedMilliseconds > SlowMs ? LogLevel.Warning
            : LogLevel.Information;

        // path only, the query string can carry tokens
        _logger.Log(level, error,
            "HTTP {Method} {Path} {StatusCode} {ElapsedMs}ms cid={CorrelationId} req={RequestBody} res={ResponseBody}",
            ctx.Request.Method, ctx.Request.Path.Value, status, sw.ElapsedMilliseconds,
            correlationId, requestBody, responseBody);
    }

    private static string GetCorrelationId(HttpContext ctx)
    {
        // only accept a simple id from the caller, so nobody can inject junk into our logs
        var incoming = ctx.Request.Headers[HeaderName].ToString();
        return SafeId.IsMatch(incoming) ? incoming : Guid.NewGuid().ToString("N");
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest req, CancellationToken ct)
    {
        if (!IsJson(req.ContentType)) return null;

        var len = req.ContentLength;
        if (len == 0) return null;
        // unknown or large size: don't buffer, let it stream straight through
        if (len is null || len > MaxBufferedRequestBytes) return "[not logged: size unknown or too large]";

        req.EnableBuffering();
        try
        {
            var buffer = new byte[(int)Math.Min(len.Value, MaxLoggedBytes)];
            var read = await req.Body.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, ct);

            var text = BodyRedactor.Redact(Encoding.UTF8.GetString(buffer, 0, read));
            return len > MaxLoggedBytes ? text + "...[truncated]" : text;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            req.Body.Position = 0;   // give the body back so the controller can read it
        }
    }

    private static async Task WriteErrorAsync(HttpContext ctx, string correlationId)
    {
        if (ctx.Response.HasStarted) return;   // too late to change the response

        ctx.Response.Clear();
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = "server_error",
            message = "Something went wrong",   // no stack trace for the client
            correlationId
        }, CancellationToken.None);
    }

    private static bool IsJson(string? contentType) =>
        contentType != null && contentType.Contains("json", StringComparison.OrdinalIgnoreCase);
}
