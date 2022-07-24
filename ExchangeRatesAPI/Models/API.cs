using System;
using System.Collections.Generic;

namespace ExchangeRatesAPI.Models
{
#pragma warning disable IDE1006 // Naming Styles

    public class API
    {
        public IList<DataSet> dataSets { get; set; }
        public Structure structure { get; set; }
    }

    public class DataSet
    {
        public IDictionary<string, Series> series { get; set; }
    }
    public class Series
    {
        public IDictionary<string, IList<double>> observations { get; set; }
    }

    public class Structure
    {
        public Dimensions dimensions { get; set; }
    }
    public class Dimensions
    {
        public IList<Observation> observation { get; set; }
        public IList<SeriesDescriptor> series { get; set; }
    }
    public class Observation
    {
        public string role { get; set; }
        public IList<Value> values { get; set; }
    }
    public class Value
    {
        public DateTime start { get; set; }
    }
    public class SeriesDescriptor
    {
        public string id { get; set; }
        public IList<ValueDescriptor> values { get; set; }
    }
    public class ValueDescriptor
    {
        public string id { get; set; }
    }

#pragma warning restore IDE1006 // Naming Styles
}
