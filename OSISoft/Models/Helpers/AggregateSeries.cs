using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LibreStream.Api.Models
{
    public class AggregateSeries
    {
        public List<string> TimeSeriesId { get; set; }
        public SearchSpan SearchSpan { get; set; }
        public string Interval { get; set; }
        public List<string> ProjectedVariables { get; set; }
        public JObject InlineVariables { get; set; }
    }
}
