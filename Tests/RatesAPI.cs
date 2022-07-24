using ExchangeRatesAPI;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class RatesAPI
    {
        // Remeber to flush the table with tokens or to replace this string with your API key.
        private const string apiKey = "testECB";

        private readonly WebApplicationFactory<Startup> application;
        private readonly HttpClient client;

        public RatesAPI()
        {
            application = new WebApplicationFactory<Startup>().WithWebHostBuilder(builder => builder.UseSetting("https_port", "5001"));
            client = application.CreateClient();
        }

        private HttpRequestMessage NewRequestMessage(DateTime start, DateTime stop, Dictionary<string, string> currencies)
        {
            var uriBuilder = new UriBuilder(client.BaseAddress.ToString() + "Rates")
            {
                Query = new QueryBuilder(new Dictionary<string, string>
                {
                    { "apiKey", apiKey },
                    { "startDate", start.ToString("yyyy-MM-dd") },
                    { "endDate", stop.ToString("yyyy-MM-dd")},
                }).ToQueryString().Value,
                Scheme = "https",
                Port = 5001,
            };
            var ret = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri)
            {
                Content = new StringContent(JsonSerializer.Serialize(currencies), Encoding.UTF8, "application/json"),
            };
            return ret;
        }

        private JsonElement GetByDate(JsonElement element, DateTime date) => element.EnumerateArray().Where(x => GetDate(x).Equals(date)).Single();
        private DateTime GetDate(JsonElement element) => element.GetProperty("date").GetDateTime();
        private JsonElement GetCurrencies(JsonElement element) => element.GetProperty("currencies")[0];
        private JsonElement GetRates(JsonElement element) => element.GetProperty("rates");
        private double GetRate(JsonElement element, string currency) =>
            element.EnumerateArray().Where(x => x.GetProperty("currencyName").GetString().Equals(currency)).Single().GetProperty("rate").GetDouble();

        [TestMethod]
        public async Task RatesTest()
        {
            var response = await client.SendAsync(NewRequestMessage(
                new DateTime(2022, 2, 3),
                new DateTime(2022, 2, 8),
                new Dictionary<string, string>{
                    { "PLN", "EUR" },
                    { "USD", "EUR" },
                }));
            Assert.IsTrue(response.StatusCode.Equals(HttpStatusCode.OK));
            string serverResponse = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(serverResponse).RootElement;

            var rateThursday = GetRate(GetRates(GetCurrencies(GetByDate(doc, new DateTime(2022, 2, 3)))), "PLN");
            var rateFriday = GetRate(GetRates(GetCurrencies(GetByDate(doc, new DateTime(2022, 2, 4)))), "PLN");
            var rateSaturday = GetRate(GetRates(GetCurrencies(GetByDate(doc, new DateTime(2022, 2, 5)))), "PLN");
            var rateSunday = GetRate(GetRates(GetCurrencies(GetByDate(doc, new DateTime(2022, 2, 6)))), "PLN");
            var rateMonday = GetRate(GetRates(GetCurrencies(GetByDate(doc, new DateTime(2022, 2, 7)))), "PLN");
            var rateTuesday = GetRate(GetRates(GetCurrencies(GetByDate(doc, new DateTime(2022, 2, 8)))), "PLN");

            var rateUsd = GetRate(GetRates(GetCurrencies(GetByDate(doc, new DateTime(2022, 2, 8)))), "USD");

            Assert.AreEqual(rateThursday, 4.5315);
            Assert.AreEqual(rateFriday, rateSaturday);
            Assert.AreEqual(rateSaturday, rateSunday);
            Assert.AreEqual(rateSunday, 4.5474);
            Assert.AreEqual(rateMonday, 4.5432);
            Assert.AreEqual(rateTuesday, 4.5312);

            Assert.AreEqual(rateUsd, 1.1408);
        }

        [TestMethod]
        public async Task SpeedTest()
        {
            // Database should be flushed from time to time for this test to work properly.
            // We try to fetch the same random rates twice.

            var random = new Random();

            for (int i = 0; i < 10; i++)
            {
                int day = random.Next(1, 29), month = random.Next(1, 13), year = random.Next(2012, 2021);

                Stopwatch sw = new Stopwatch();

                sw.Start();
                await client.SendAsync(NewRequestMessage(
                    new DateTime(year, month, day),
                    new DateTime(year, month, day),
                    new Dictionary<string, string>{
                    { "PLN", "EUR" },
                    { "USD", "EUR" },
                    }));
                sw.Stop();

                var firstMs = sw.ElapsedMilliseconds;

                sw.Restart();
                await client.SendAsync(NewRequestMessage(
                    new DateTime(year, month, day),
                    new DateTime(year, month, day),
                    new Dictionary<string, string>{
                    { "PLN", "EUR" },
                    { "USD", "EUR" },
                    }));
                sw.Stop();

                var secondMs = sw.ElapsedMilliseconds;

                Assert.IsTrue(firstMs > secondMs); // We assume that cached calls are faster than calls to the external API.
            }
        }
    }
}
