using LibreStream.Api.Models;
using Newtonsoft.Json.Linq;

namespace LibreStream.OSISoft.Models
{
    public class QueryEventPayload
    {
        public GetEvents GetEvents { get; set; }
        public JObject ServiceProperties { get; set; }
        public AggregateSeries AggregateSeries { get; set; }
    }
}
