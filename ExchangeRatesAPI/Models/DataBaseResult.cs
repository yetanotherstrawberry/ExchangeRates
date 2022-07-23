using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
    public enum Result
    {
        Complete,
        Incomplete,
    }

    public class DatabaseResult
    {
        public Result Result { get; set; }
        public IEnumerable<Exchange> Items { get; set; }
    }
}
