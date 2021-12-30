using LibreStream.Api.Iot.Services;
using LibreStream.OSISoft.Models;
using Newtonsoft.Json;

namespace LibreStream.Api.Iot.Tests
{
    class TestMeasurementService : IMeasurementService
	{
		public QueryResultPage QueryAsync(QueryEventPayload query)
		{
			return JsonConvert.DeserializeObject<QueryResultPage>(TestConstants.IOT_QUERY_RESULT);
		}
	}
}
