using System;

namespace ExchangeRatesAPI.Models
{
    public class Request
    {
        public string UrlPath { get; set; }
        public string UrlQuery { get; set; }
        public string RemoteAddr { get; set; }
        public DateTime RequestDate { get; set; }
    }
}
