using System;
using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
#pragma warning disable IDE1006 // Naming Styles

    public class API
    {
        public DataSet[] dataSets { get; set; }
        public Structure structure { get; set; }
    }

    public class DataSet
    {
        public Dictionary<string, Series> series { get; set; }
    }
    public class Series
    {
        public Dictionary<string, double[]> observations { get; set; }
    }

    public class Structure
    {
        public Dimensions dimensions { get; set; }
    }
    public class Dimensions
    {
        public Observation[] observation { get; set; }
        public SeriesDescriptor[] series { get; set; }
    }
    public class Observation
    {
        public string role { get; set; }
        public Value[] values { get; set; }
    }
    public class Value
    {
        public DateTime start { get; set; }
    }
    public class SeriesDescriptor
    {
        public string id { get; set; }
        public ValueDescriptor[] values { get; set; }
    }
    public class ValueDescriptor
    {
        public string id { get; set; }
    }

#pragma warning restore IDE1006 // Naming Styles
}
