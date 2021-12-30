using System.Collections.Generic;

namespace LibreStream.Api.Models
{
    public class Property
    {
        public List<object> Values { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}
