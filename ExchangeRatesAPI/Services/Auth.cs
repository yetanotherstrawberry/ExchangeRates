using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ExchangeRatesAPI.Services
{
    public class Auth
    {
        private readonly ApplicationDbContext db;

        public Auth(ApplicationDbContext context)
        {
            db = context;
        }

        public async Task<bool> IsAuthorizedAsync(string apiKey)
        {
            var dbLastApiKey = await db.Tokens.OrderBy(x => x.Created).LastAsync();

            if (dbLastApiKey.Key.Equals(apiKey)) return true; // Newest token provided - access granted.
            else return false;
        }
    }
}
