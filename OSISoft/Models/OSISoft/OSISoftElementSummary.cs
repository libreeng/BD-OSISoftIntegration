using LibreStream.Api.Models;
using System.Collections.Generic;

namespace LibreStream.OSISoft.Models
{
    public class OSISoftElementSummary
    {
        public Links Links { get; set; }
        public List<SummaryItem> Items { get; set; }
    }
}
