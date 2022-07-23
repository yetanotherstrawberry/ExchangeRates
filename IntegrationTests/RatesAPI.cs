using ExchangeRatesAPI;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
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
        private const string apiKey = "apiTest";

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

        [TestMethod]
        public async Task Test()
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

            var rateThursday = doc[0].GetProperty("currencies")[0].GetProperty("rates")[0].GetProperty("rate").GetDouble();
            var rateFriday = doc[1].GetProperty("currencies")[0].GetProperty("rates")[0].GetProperty("rate").GetDouble();
            var rateSaturday = doc[2].GetProperty("currencies")[0].GetProperty("rates")[0].GetProperty("rate").GetDouble();
            var rateSunday = doc[3].GetProperty("currencies")[0].GetProperty("rates")[0].GetProperty("rate").GetDouble();
            var rateMonday = doc[4].GetProperty("currencies")[0].GetProperty("rates")[0].GetProperty("rate").GetDouble();
            var rateTuesday = doc[5].GetProperty("currencies")[0].GetProperty("rates")[0].GetProperty("rate").GetDouble();

            var rateUsd = doc[5].GetProperty("currencies")[0].GetProperty("rates")[1].GetProperty("rate").GetDouble();

            Assert.AreEqual(rateThursday, 4.5315);
            Assert.AreEqual(rateFriday, rateSaturday);
            Assert.AreEqual(rateSaturday, rateSunday);
            Assert.AreEqual(rateSunday, 4.5474);
            Assert.AreEqual(rateMonday, 4.5432);
            Assert.AreEqual(rateTuesday, 4.5312);

            Assert.AreEqual(rateUsd, 1.1408);
        }
    }
}
