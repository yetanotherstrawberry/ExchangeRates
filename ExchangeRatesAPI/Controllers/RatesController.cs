using ExchangeRatesAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
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
        private readonly ILogger<RatesController> logger;
        private readonly ApplicationDbContext db;
        private readonly HttpClient http;

        // Just in case the startDate is a day without exchange rates,
        // how many days in the past should we check for the last known rate? (will always use last)
        private const int DaysFiller = 5;

        public RatesController(ApplicationDbContext context, HttpClient httpClient, ILogger<RatesController> ilogger)
        {
            db = context;
            http = httpClient;
            logger = ilogger;
        }

        private string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd");

        private async Task<DatabaseResult> GetRecordsFromDatabase(
            Dictionary<string, string> currencyCodes,
            DateTime startDate,
            DateTime endDate)
        {
            var result = Result.Complete;

            // AsNoTracking() is neccessary, because we've previously loaded collections in this context,
            // so Where() clause wouldn't work. Also it's faster.
            var items = await db.Exchanges.AsNoTracking()
                .Where(exchange => exchange.Date >= startDate && exchange.Date <= endDate)
                .Include(currency => currency.Currencies.Where(currency => currencyCodes.Values.Contains(currency.Denominator)))
                .ThenInclude(x => x.Rates.Where(rate => currencyCodes.Keys.Contains(rate.CurrencyName))).AsSplitQuery().ToListAsync();

            foreach (var exchange in items)
            {
                if (result.Equals(Result.Incomplete)) break; // We are missing too much data - use external API.

                foreach (var currency in exchange.Currencies)
                {
                    currency.Rates.RemoveAll(rate => !currencyCodes[rate.CurrencyName].Equals(currency.Denominator));
                    if (!currencyCodes.Keys.All(x => currency.Rates.Any(y => y.CurrencyName.Equals(x))))
                    {
                        result = Result.Incomplete; // We are missing a rate.
                    }
                }
            }

            return new DatabaseResult
            {
                Items = items,
                Result = result,
            };
        }

        private async Task AddRecordsToDatabase(IEnumerable<Exchange> days)
        {
            foreach (var day in days)
            {
                var dbDay = await db.Exchanges
                    .Include(x => x.Currencies)
                    .ThenInclude(x => x.Rates)
                    .OrderBy(x => x.Date) // Only to remove warning about using AsSplitQuery() is to sort the input.
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

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when ((ex.InnerException as SqlException)?.Number == 2627)
            {
                // Someone else just (concurrently?) added some rate(s). It's okay - just discard it.
                logger.LogWarning(Properties.Resources.ERROR_ITEM_ALREADY_IN_DB);
                LogExceptions(ex);
            }
        }

        private void LogExceptions(Exception ex)
        {
            logger.LogInformation(Properties.Resources.ERR_SHOWING_EXC);
            for (Exception exc = ex; exc != null; exc = exc.InnerException)
            {
                logger.LogWarning(exc.Message);
            }
        }

        private string AggregateStrings(IEnumerable<string> strings) => strings.Aggregate((x, y) => x + "+" + y);

        private void AddMissingData(ICollection<Exchange> days, DateTime start, DateTime end, Dictionary<string, string> currencyCodes)
        {
            var discDays = days.ToDictionary(x => x.Date);
            var minDate = discDays.Keys.Min();

            for (DateTime day = start; day <= end; day = day.AddDays(1))
            {
                if (discDays.ContainsKey(day))
                {
                    var exchange = discDays[day];

                    foreach (var denominator in currencyCodes.Values)
                    {
                        var currency = exchange.Currencies.SingleOrDefault(x => x.Denominator.Equals(denominator));
                        if (currency != null)
                        {
                            foreach (var nominator in currencyCodes.Keys)
                            {
                                var rate = currency.Rates.SingleOrDefault(x => x.CurrencyName.Equals(nominator));
                                if (rate == null)
                                {
                                    double newRate = 0;
                                    for (DateTime innerDate = day; innerDate >= minDate; innerDate = innerDate.Subtract(TimeSpan.FromDays(1)))
                                    {
                                        if (!discDays.Keys.Contains(innerDate)) continue; // Consecutive missing days.

                                        var innerRate = discDays[innerDate].Currencies
                                            .SingleOrDefault(x => x.Denominator.Equals(denominator))?
                                            .Rates?.SingleOrDefault(x => x.CurrencyName.Equals(nominator))?.Rate;

                                        if (innerRate != null)
                                        {
                                            newRate = innerRate.Value;
                                            break;
                                        }
                                    }

                                    if (newRate == 0)
                                        throw new ArgumentOutOfRangeException(paramName: nameof(days),
                                            string.Format(Properties.Resources.ERROR_API_NO_DATA, DaysFiller));

                                    currency.Rates.Add(new ExchangeRate
                                    {
                                        CurrencyName = nominator,
                                        Rate = newRate,
                                    });
                                }
                                // else => we are good.
                            }
                        }
                        else
                        {
                            exchange.Currencies.Add(new Currency
                            {
                                Denominator = denominator,
                                Rates = new List<ExchangeRate>(),
                            });
                            // In order to copy contents, we want to execute loop on this day once again.
                            day = day.Subtract(TimeSpan.FromDays(1));
                            continue;
                        }
                    }
                }
                else
                {
                    var newExchange = new Exchange
                    {
                        Date = day,
                        Currencies = new List<Currency>(),
                    };
                    days.Add(newExchange);
                    discDays.Add(newExchange.Date, newExchange);

                    day = day.Subtract(TimeSpan.FromDays(1)); // In order to copy contents, we want to execute loop on this day once again.
                    continue;
                }
            }
        }

        private async Task<ICollection<Exchange>> ExternalGetExchangeRates(DateTime startDate, DateTime endDate, Dictionary<string, string> currencyCodes)
        {
            var apiUrl = string.Format(Properties.Resources.API_ENDPOINT, AggregateStrings(currencyCodes.Keys), AggregateStrings(currencyCodes.Values), FormatDate(startDate), FormatDate(endDate));
            var content = (await http.GetAsync(apiUrl)).Content;
            if (content == null) return new List<Exchange>(); // No data.
            var api = await JsonSerializer.DeserializeAsync<API>(await content.ReadAsStreamAsync());

            var dates = api.structure.dimensions.observation.Single(x => x.role.Equals("time")).values.Select(x => x.start.Date).ToList();

            var currencyDesc = api.structure.dimensions.series.Single(x => x.id.Equals("CURRENCY"));
            // Array of currencies. Index is equal to the appropriate code in the dataSet's name - read below.
            var currenciesNames = currencyDesc.values.Select(x => x.id).ToList();
            // API returns dataSets named like 0:0:0:0. This variable indicates which number represents the currency name.
            var currencyColumn = Array.IndexOf(api.structure.dimensions.series, currencyDesc);

            var currencyDenomDesc = api.structure.dimensions.series.Single(x => x.id.Equals("CURRENCY_DENOM"));
            var currencyDenomsNames = currencyDenomDesc.values.Select(x => x.id).ToList();
            var currencyDenomColumn = Array.IndexOf(api.structure.dimensions.series, currencyDenomDesc);

            var days = Enumerable.Repeat<Exchange>(null, dates.Count).ToList();

            for (var day = 0; day < days.Count; day++) // Fill days and currencies.
            {
                days[day] = new Exchange
                {
                    Date = dates[day],
                    // Create list with nulls to allow random index access.
                    Currencies = new List<Currency>(Enumerable.Repeat<Currency>(null, currencyDenomsNames.Count)),
                };

                for (var denominatorId = 0; denominatorId < days[day].Currencies.Count; denominatorId++)
                {
                    var currency = new Currency
                    {
                        Rates = new List<ExchangeRate>(Enumerable.Repeat<ExchangeRate>(null, currenciesNames.Count)),
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
                    // Name of the series splitted to dimensions.
                    var keySplit = series.Key.Split(':').Select(x => int.Parse(x)).ToArray();

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
        [ResponseCache(
            Duration = 2880,
            Location = ResponseCacheLocation.Any,
            VaryByQueryKeys = new string[] {
                "currencyCodes",
                "startDate",
                "endDate",
        })]
        public async Task<IActionResult> Get(
            [FromBody] Dictionary<string, string> currencyCodes,
            DateTime startDate,
            DateTime endDate
        )
        {
            try
            {
                startDate = startDate.Date; // Remove time - leave only date.
                endDate = endDate.Date;

                if (startDate >= DateTime.Today || endDate >= DateTime.Today || startDate > endDate) return NotFound();

                var dbResult = await GetRecordsFromDatabase(currencyCodes, startDate, endDate);

                if (dbResult.Result.Equals(Result.Complete))
                {
                    return Ok(dbResult.Items);
                }
                else
                {
                    // If rates from startDate or endDate are unknown we will use values from previous/future days.
                    var futureDay = endDate.AddDays(DaysFiller);
                    var yesterday = DateTime.Today.Subtract(TimeSpan.FromDays(1));
                    if (futureDay > yesterday) futureDay = yesterday;
                    var pastDay = startDate.Subtract(TimeSpan.FromDays(DaysFiller));

                    var externalDb = await ExternalGetExchangeRates(pastDay, futureDay, currencyCodes);
                    AddMissingData(externalDb, startDate, endDate, currencyCodes);
                    await AddRecordsToDatabase(externalDb);
                    return Ok(externalDb.Where(x => x.Date <= endDate && x.Date >= startDate).OrderBy(x => x.Date));
                }
            }
            catch (Exception e)
            {
                logger.LogWarning(e.Message);
                return StatusCode(500);
            }
        }
    }
}
