using System;
using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
    public class Exchange
    {
        public DateTime Date { get; set; }
        public List<Currency> Currencies { get; set; }
    }
}
