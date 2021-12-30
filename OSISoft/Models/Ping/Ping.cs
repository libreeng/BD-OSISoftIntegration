using System;

namespace LibreStream.Api.Common.Models
{
    /// <summary>
    /// Entity used as response to ping request
    /// </summary>
    public class Ping
	{
		public Version AssemblyVersion { get; set; }
		public string ApplicationName { get; set; }
		public long TimestampUtc { get; set; }
		public string Environment { get; set; }
	}
}
