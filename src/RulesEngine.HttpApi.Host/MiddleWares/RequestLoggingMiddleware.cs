using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp.Users;

namespace RulesEngine.MiddleWares;
 
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICurrentUser _currentUser;
    public RequestLoggingMiddleware(RequestDelegate next, ICurrentUser currentUser)
    {
        _next = next;
        _currentUser = currentUser;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        //     context.Request.EnableBuffering();

        try
        {


            if (context.Request.Path.HasValue && (context.Request.Path.Value.Contains("swagger") || context.Request.Path.Value.Contains("hc") || context.Request.Method.ToLower() == "Get".ToLower()) || context.Request.Path.Value.Contains("/signalr/negotiate") || context.Request.Path.Value.Contains("/connect"))
            {
                await _next(context);
            }
            else
            {

                var correlationIds = context.Request.Headers["CorrelationId"];
                var correlationId = correlationIds.FirstOrDefault();
                context.Items["CorrelationId"] = correlationId;
                var userId = _currentUser.Id?.ToString() ?? "Anonymous";
                string requestBody = await ReadRequestBody(context.Request);


                Log.ForContext("CorrelationId", correlationId)
                 .ForContext("UserId", userId) // 👈 Add user id here
                 .ForContext("RequestBody", requestBody, destructureObjects: true)
                 .Warning("Incoming request {@HttpRequestInfo}",
                     new
                     {
                         Method = context.Request.Method,
                         Path = context.Request.Path.Value,
                         IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                         Host = context.Request.Host.Value,
                         Scheme = context.Request.Scheme,
                         Protocol = context.Request.Protocol
                     });

                await _next(context); // run the rest of the pipeline


            }
        }
        catch (Exception ex)
        {
            await _next(context);

        }


    }



    private async Task<string> ReadRequestBody(HttpRequest request)
    {

        string body = "";
        try
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            body = Encoding.UTF8.GetString(buffer);
        }
        finally
        {
            request.Body.Position = 0;
        }
        return body;
    }
    private async Task<string> FormatResponse(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var text = await new StreamReader(response.Body).ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        return text;
    }


}