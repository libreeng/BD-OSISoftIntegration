using LibreStream.OSISoft.Models;

namespace LibreStream.Api.Iot.Services
{
    public interface IMeasurementService
	{
		/// <summary>
		/// Return query results based on query
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		QueryResultPage QueryAsync(QueryEventPayload query);
	}
}
