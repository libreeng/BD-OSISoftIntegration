using System.Collections.Generic;

namespace LibreStream.Api.Models
{
    public class GetEvents
    {
        public List<string> TimeSeriesId { get; set; }
        public SearchSpan SearchSpan { get; set; }
        public List<ProjectedProperty> ProjectedProperties { get; set; }
        public int Take { get; set; }
    }
}
