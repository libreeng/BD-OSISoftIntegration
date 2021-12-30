using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LibreStream.Api.Models
{
    public class SummaryItem
    {
        public string Type { get; set; }
        public string WebId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public Links Links { get; set; }
        public List<JObject> Items { get; set; }
    }
}
