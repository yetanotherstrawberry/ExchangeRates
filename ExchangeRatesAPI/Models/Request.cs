using System;
using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
    public class Request
    {
#pragma warning disable IDE1006 // Naming Styles
        public string currencyCodes { get; set; }
        public string currencyDenomCodes { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public ApiKey apiKey { get; set; }
        public DateTime RequestDate { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}
