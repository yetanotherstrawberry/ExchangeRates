using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeRatesAPI.Middleware
{
    public class Auth
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<Auth> logger;
        private const string apiVarName = "apiKey", defaultApiKey = "testECB";

        public Auth(RequestDelegate next, ILogger<Auth> ilogger)
        {
            _next = next;
            logger = ilogger;
        }

        private void LogFailedAuth(string message, HttpContext context) =>
            logger.LogWarning(DateTime.Now.ToString() + " / " + context.Connection.RemoteIpAddress.ToString() + ": " + message);

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
        {
            if (!context.Request.Query.Keys.Contains(apiVarName) || string.IsNullOrEmpty(context.Request.Query[apiVarName].ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync(Properties.Resources.ERROR_NO_KEY);
                LogFailedAuth(Properties.Resources.ERROR_NO_KEY, context);
                return;
            }

            var apiKey = context.Request.Query[apiVarName].ToString();
            var dbLastApiKey = await db.Tokens.OrderBy(x => x.Created).LastOrDefaultAsync();

            if (!apiKey.Equals(dbLastApiKey?.Key))
            {
                if (dbLastApiKey != null || !apiKey.Equals(defaultApiKey)) // If there are no API keys in the database, we allow the default one.
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync(Properties.Resources.ERROR_BAD_KEY);
                    LogFailedAuth(Properties.Resources.ERROR_BAD_KEY, context);
                    return;
                }
            }

            await _next(context);
        }
    }
}
