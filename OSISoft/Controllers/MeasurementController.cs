using LibreStream.Api.Common.Controllers;
using LibreStream.Api.Common.Middleware;
using LibreStream.Api.Iot.Models;
using LibreStream.Api.Iot.Services;
using LibreStream.OSISoft.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace LibreStream.Api.Iot.Controllers
{
    [Route("api/iot/[controller]")]
	[ApiController]
	[Consumes("application/json")]
	[Produces("application/json")]
	[ApiExplorerSettings(GroupName = "iot")]
	public class MeasurementController : OSISoftControllerBase
	{
		private readonly IMeasurementService service;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="service"></param>
		public MeasurementController(IMeasurementService service)
		{
			this.service = service;
		}

		/// <summary>
		/// Post an advanced event query and return the query response
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		[HttpPost]
		[Route("query")]
		[Authorize(Policy = "HasIoTMeasurementParams")]
		public ActionResult<QueryResultPage> PostQuery([FromBody] QueryEventPayload query)
		{
			try
			{
				query.ServiceProperties ??= GetServiceProperties().Properties;
				if (QueryCheck(query))
				{
					QueryResultPage result = service.QueryAsync(query);
					return Ok(result);
				}
				return BadRequest("Service properties are invalid");
			}
			catch (Exception ex)
			{
				return HandleException(ex);
			}
		}

		/// <summary>
		/// Get the properties for this service
		/// </summary>
		/// <returns></returns>
		private IoTMeasurementServiceParameters GetServiceProperties()
		{
			return JsonConvert.DeserializeObject<IoTMeasurementServiceParameters>(HttpContext.User.Claims.FirstOrDefault(c => c.Type == SessionTokenAuthenticationHandler.IOT_MEASUREMENT_CLAIM)?.Value);
		}

		private bool QueryCheck(QueryEventPayload query)
        {
			return query.ServiceProperties != null && query.ServiceProperties["baseAddress"] != null && query.ServiceProperties["username"] != null && query.ServiceProperties["password"] != null;
		}
	}
}
