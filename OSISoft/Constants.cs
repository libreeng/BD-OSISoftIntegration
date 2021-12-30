using System.Collections.Generic;

namespace LibreStream.Api
{
    public static class Constants
	{
		public static class ServiceConstants
        {
			public static readonly Dictionary<string, string> AGGREGATE_FUNCTION_MAPPER = new Dictionary<string, string>(){
				{ "min($value)" , "Minimum" },
				{ "max($value)" , "Maximum"  },
				{ "sum($value)" , "Total"  },
				{ "avg($value)" , "Average"  },
				{ "stdev($value)" , "StdDev"  },
			};

		}
	}
}
