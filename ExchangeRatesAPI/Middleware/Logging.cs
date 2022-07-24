using ExchangeRatesAPI.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace ExchangeRatesAPI.Middleware
{
    public class RequestLogging
    {
        private readonly RequestDelegate _next;

        public RequestLogging(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext db)
        {
            await db.Requests.AddAsync(new Request
            {
                RequestDate = DateTime.Now,
                UrlPath = context.Request.Path.ToString(),
                UrlQuery = context.Request.QueryString.ToString(),
                // RemoteIpAddress is null when the API is accessed by tests.
                RemoteAddr = context.Connection.RemoteIpAddress?.ToString(),
            });
            await db.SaveChangesAsync();
            await _next(context);
        }
    }
}
