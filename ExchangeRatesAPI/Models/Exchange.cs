using System;
using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
    public class Exchange
    {
        public DateTime Date { get; set; }
        public ICollection<Currency> Currencies { get; set; }
    }
}
