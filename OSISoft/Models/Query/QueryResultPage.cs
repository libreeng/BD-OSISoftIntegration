using LibreStream.Api.Models;
using System.Collections.Generic;

namespace LibreStream.OSISoft.Models
{
    public class QueryResultPage
    {
        public List<string> Timestamps { get; set; }
        public List<Property> Properties { get; set; }
        public double Progress { get; set; }
    }

}

