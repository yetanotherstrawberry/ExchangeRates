using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
    public class Currency
    {
        public string Denominator { get; set; }
        public List<ExchangeRate> Rates { get; set; }
    }
}
