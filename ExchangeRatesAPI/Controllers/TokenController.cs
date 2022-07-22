using ExchangeRatesAPI.Models;
using ExchangeRatesAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ExchangeRatesAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TokenController : Controller
    {
        private readonly ILogger<RatesController> _logger;
        private readonly ApplicationDbContext db;
        private readonly Auth auth;

        public TokenController(ILogger<RatesController> logger, ApplicationDbContext context, Auth auth)
        {
            _logger = logger;
            db = context;
            this.auth = auth;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Incorrect key
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Any exception
        public async Task<IActionResult> Get(string apiKey)
        {
            ApiKey ret = null;

            try
            {
                if (!await auth.IsAuthorizedAsync(apiKey)) return Unauthorized();

                ret = new ApiKey
                {
                    Key = Guid.NewGuid().ToString(),
                    Created = DateTime.Now,
                };

                await db.Tokens.AddAsync(ret);
                await db.SaveChangesAsync();

                return Ok(ret.Key);
            }
            catch (Exception e)
            {
                try
                {
                    if (ret != null)
                    {
                        var addedToken = await db.Tokens.FindAsync(ret.Key);
                        if (addedToken != null)
                        {
                            db.Tokens.Remove(addedToken);
                            await db.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception inner)
                {
                    _logger.LogError(inner.Message);
                }

                _logger.LogError(e.Message);
                return StatusCode(500);
            }
        }
    }
}
