using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace RulesEngine.MiddleWares
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {

            var header = context.Request.Headers["CorrelationId"];

            string sessionId;

            if (header.Count > 0)
            {
                sessionId = header[0];
            }
            else
            {
                sessionId = Guid.NewGuid().ToString();
            }
            //CorrelationContext.SetCorrelationId(sessionId);
            context.Request.Headers["CorrelationId"] = sessionId;
            await _next(context);


        }
    }
}
