using Microsoft.AspNetCore.Http;
using Serilog;
using System.IO;
using System.Threading.Tasks;

namespace RulesEngine.MiddleWares;

public class ResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public ResponseLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Path.HasValue && (context.Request.Path.Value.Contains("swagger") || context.Request.Path.Value.Contains("hc")) || context.Request.Path.Value.Contains("/signalr/negotiate") || context.Request.Path.Value.Contains("/connect"))
        {
            await _next(context);
        }
        else
        {
            var originalBody = context.Response.Body;

            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            await _next(context);

            buffer.Seek(0, SeekOrigin.Begin);
            var contentType = context.Response.ContentType ?? string.Empty;
            var correlationId = context.Items["CorrelationId"]?.ToString();

            if (IsTextBasedContentType(contentType))
            {
                var responseBody = await new StreamReader(buffer).ReadToEndAsync();
                if (context.Response.StatusCode >= 400)
                {
                    Log.ForContext("CorrelationId", correlationId)
                  .ForContext("ResponseBody", responseBody, destructureObjects: true)
                  .Error("Outgoing response Error {@HttpResponseInfo}",
                      new
                      {
                          StatusCode = context.Response.StatusCode,
                          Method = context.Request.Method,
                          Path = context.Request.Path.Value
                      });

                }
                else
                {
                    Log.ForContext("CorrelationId", correlationId)
                   .ForContext("ResponseBody", responseBody, destructureObjects: true)
                   .Warning("Outgoing response {@HttpResponseInfo}",
                       new
                       {
                           StatusCode = context.Response.StatusCode,
                           Method = context.Request.Method,
                           Path = context.Request.Path.Value
                       });
                }

            }
            else
            {
                // Log metadata only for binary responses
                Log.ForContext("CorrelationId", correlationId)
                   .Warning("Outgoing response {@HttpResponseInfo}",
                      new
                      {
                          StatusCode = context.Response.StatusCode,
                          Method = context.Request.Method,
                          Path = context.Request.Path.Value
                      });
            }
            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;
        }
    }
    private static bool IsTextBasedContentType(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return false;

        return contentType.StartsWith("text/") ||
               contentType.Contains("application/json") ||
               contentType.Contains("application/xml") ||
               contentType.Contains("application/javascript");
    }
}
