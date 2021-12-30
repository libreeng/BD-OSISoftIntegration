using LibreStream.Api.Models;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LibreStream.OSISoft.Models
{
    public class OSISoftQueryResult
    {
        public Links Links { get; set; }
        public List<JObject> Items { get; set; }
    }
}
