using LibreStream.Api.Models;
using System.Collections.Generic;

namespace LibreStream.OSISoft.Models
{
    public class OSISoftDevices
    {
        public Links Links { get; set; }
        public List<OSISoftElement> Items { get; set; }
    }
}
