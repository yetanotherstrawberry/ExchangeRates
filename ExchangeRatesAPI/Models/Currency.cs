using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
    public class Currency
    {
        public string Denominator { get; set; }
        public IList<ExchangeRate> Rates { get; set; }
    }
}
