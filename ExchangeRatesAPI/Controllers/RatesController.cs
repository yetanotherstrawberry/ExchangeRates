using ExchangeRatesAPI.Models;
using ExchangeRatesAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ExchangeRatesAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RatesController : ControllerBase
    {
        private readonly ILogger<RatesController> _logger;
        private readonly ApplicationDbContext db;
        private readonly HttpClient http;
        private readonly Auth auth;

        // Just in case the startDate is a day without exchange rates, how many days in the past should we check for the last known rate? (will always use last)
        private const int DaysFiller = 5;

        public RatesController(ILogger<RatesController> logger, ApplicationDbContext context, HttpClient http, Auth auth)
        {
            _logger = logger;
            db = context;
            this.http = http;
            this.auth = auth;
        }

        private string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");

        private async Task<IEnumerable<Exchange>> GetRecordsFromDatabase(
            Dictionary<string, string> currencyCodes,
            DateTime startDate,
            DateTime endDate)
        {

            var items = await (db.Exchanges.AsNoTracking() // This is neccessary, because we've previously loaded collections in this context, so Where() clause wouldn't work.
                .Where(exchange => exchange.Date >= startDate && exchange.Date <= endDate)
                .Include(currency => currency.Currencies.Where(currency => currencyCodes.Values.Contains(currency.Denominator)))
                .ThenInclude(x => x.Rates.Where(rate => currencyCodes.Keys.Contains(rate.CurrencyName))).AsSplitQuery()).ToListAsync();

            // This will filter unnecessary rates - it needs to be done locally, because the dictionary cannot be embedded inside SQL.
            items
                .ForEach(exchange => exchange.Currencies
                .ForEach(currency => currency.Rates = currency.Rates
                .Where(rate => currencyCodes[rate.CurrencyName].Equals(currency.Denominator))
                .ToList()));

            return items;
        }

        private async Task AddRecordsToDatabase(IEnumerable<Exchange> days)
        {
            foreach (var day in days)
            {
                var dbDay = await db.Exchanges
                    .Include(x => x.Currencies)
                    .ThenInclude(x => x.Rates)
                    .OrderBy(x => x.Date) // Only to remove warning about using AsSplitQuery() without sorting.
                    .AsSplitQuery()
                    .SingleOrDefaultAsync(x => x.Date.Equals(day.Date));

                if (dbDay != null)
                {

                    var dbCurrencies = dbDay.Currencies;

                    foreach (var currency in day.Currencies)
                    {
                        var dbCurrency = dbCurrencies.SingleOrDefault(x => x.Denominator.Equals(currency.Denominator));

                        if (dbCurrency != null)
                        {
                            var dbRates = dbCurrency.Rates;
                            foreach (var rate in currency.Rates)
                            {
                                if (!dbRates.Any(x => x.CurrencyName.Equals(rate.CurrencyName)))
                                    dbRates.Add(rate);
                            }
                        }
                        else dbDay.Currencies.Add(currency);
                    }

                }
                else db.Exchanges.Add(day);
            }

            await db.SaveChangesAsync();
        }

        private string AggregateStrings(IEnumerable<string> strings) => strings.Aggregate((x, y) => x + "+" + y);

        private async Task<IEnumerable<Exchange>> ExternalGetExchangeRates(DateTime startDate, DateTime endDate, Dictionary<string, string> currencyCodes)
        {
            var apiUrl = string.Format(Properties.Resources.API_ENDPOINT, AggregateStrings(currencyCodes.Keys), AggregateStrings(currencyCodes.Values), FormatDate(startDate), FormatDate(endDate));
            var content = (await http.GetAsync(apiUrl)).Content;
            if (content == null) return new List<Exchange>(); // No data.
            var api = await JsonSerializer.DeserializeAsync<API>(await content.ReadAsStreamAsync());

            var dates = api.structure.dimensions.observation.Single(x => x.role.Equals("time")).values.Select(x => x.start.Date).ToArray();

            var currencyDesc = api.structure.dimensions.series.Single(x => x.id.Equals("CURRENCY"));
            var currenciesNames = currencyDesc.values.Select(x => x.id).ToArray(); // Array of currencies. Index is equal to the appropriate code in the dataSet's name - read below.
            var currencyColumn = Array.IndexOf(api.structure.dimensions.series, currencyDesc); // API returns dataSets named like 0:0:0:0. This variable indicates which number represents the currency name.

            var currencyDenomDesc = api.structure.dimensions.series.Single(x => x.id.Equals("CURRENCY_DENOM"));
            var currencyDenomsNames = currencyDenomDesc.values.Select(x => x.id).ToArray();
            var currencyDenomColumn = Array.IndexOf(api.structure.dimensions.series, currencyDenomDesc);

            var days = new Exchange[dates.Length];

            for (var day = 0; day < days.Length; day++) // Fill days and currencies.
            {
                days[day] = new Exchange
                {
                    Date = dates[day],
                    Currencies = new List<Currency>(Enumerable.Repeat<Currency>(null, currencyDenomsNames.Length)), // Create list with nulls to allow random index access.
                };

                for (var denominatorId = 0; denominatorId < days[day].Currencies.Count; denominatorId++)
                {
                    var currency = new Currency
                    {
                        Rates = new List<ExchangeRate>(Enumerable.Repeat<ExchangeRate>(null, currenciesNames.Length)),
                    };

                    days[day].Currencies[denominatorId] = currency;

                    for (var currencyId = 0; currencyId < currency.Rates.Count; currencyId++)
                    {
                        currency.Rates[currencyId] = new ExchangeRate
                        {
                            CurrencyName = currenciesNames[currencyId],
                        };
                        currency.Denominator = currencyDenomsNames[denominatorId];
                    }
                }
            }

            foreach (var dataSet in api.dataSets)
            {
                foreach (var series in dataSet.series) // Fill exchange rates.
                {

                    var keySplit = series.Key.Split(':').Select(x => int.Parse(x)).ToArray(); // Name of the series splitted to dimensions.

                    foreach (var rate in series.Value.observations)
                    {
                        var exchange = days[int.Parse(rate.Key)];
                        var currencies = exchange.Currencies;
                        var exchangeRates = currencies[keySplit[currencyDenomColumn]].Rates;
                        exchangeRates[keySplit[currencyColumn]].Rate = rate.Value.Last(); // Avg()?
                    }

                }
            }

            return days;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Exchange>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Incorrect key
        [ProducesResponseType(StatusCodes.Status404NotFound)] // Future date or today (no rates)
        [ProducesResponseType(StatusCodes.Status500InternalServerError)] // Any exception
        public async Task<IActionResult> Get(
            [FromBody] Dictionary<string, string> currencyCodes,
            DateTime startDate,
            DateTime endDate,
            string apiKey
        )
        {
            try
            {
                if (!await auth.IsAuthorizedAsync(apiKey)) return Unauthorized();

                await db.Requests.AddAsync(new Request
                {
                    apiKey = await db.Tokens.FindAsync(apiKey),
                    currencyCodes = AggregateStrings(currencyCodes.Keys),
                    currencyDenomCodes = AggregateStrings(currencyCodes.Values),
                    startDate = startDate,
                    endDate = endDate,
                    RequestDate = DateTime.Now,
                });
                await db.SaveChangesAsync();

                startDate = startDate.Date; // Remove time - leave only date, so...
                endDate = endDate.Date;
                var noOfDays = (int)(endDate - startDate).TotalDays + 1; // ... this should be integer.

                if (startDate >= DateTime.Today || endDate >= DateTime.Today || startDate > endDate) return NotFound();


                var temp = await ExternalGetExchangeRates(startDate, endDate, currencyCodes);
                await AddRecordsToDatabase(temp);
                // TODO: Get items from database.


                return Ok(await GetRecordsFromDatabase(currencyCodes, startDate, endDate));
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e.Message);
                return StatusCode(500);
            }
        }
    }
}
