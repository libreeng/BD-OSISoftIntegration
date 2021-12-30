using LibreStream.Api.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LibreStream.OSISoft.Models
{
    public class OSISoftElement
    {
        public string WebId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public string TemplateName { get; set; }
        public bool HasChildren { get; set; }
        public List<string> CategoryNames { get; set; }
        public JObject ExtendedProperties { get; set; }
        public Links Links { get; set; }
    }
}
