using LibreStream.Api.Common.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;

namespace LibreStream.Api.Common.Controllers
{
    /// <summary>
    /// Controller to answer ping requests to check if API application is available
    /// </summary>
    [Route("api/[controller]", Name = "ping")]
	[ApiController]
	public class PingController : ControllerBase
	{
		private readonly ILogger _logger;
		private readonly IWebHostEnvironment _hostingEnvironment;

		public PingController(ILogger<PingController> logger, IWebHostEnvironment hostingEnvironment)
		{
			_logger = logger;
			_hostingEnvironment = hostingEnvironment;
		}

		/// <summary>
		/// Handle a ping request to check availability of this API application
		/// </summary>
		/// <param name="logMessage">Optional message to log</param>
		/// <param name="logLevel">Optional level of log message if provided</param>
		/// <returns>Ping object containing basic info about this application</returns>
		[HttpGet(Name = "ping")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public IActionResult Get([FromQuery]string logMessage, [FromQuery]string logLevel)
		{
			if (!string.IsNullOrEmpty(logMessage))
			{
				if (!Enum.TryParse(logLevel, true, out LogLevel level) || level > LogLevel.None)
				{
					level = LogLevel.Information;
				}
				_logger.Log(level, $"Given message: {logMessage}");
			}

			return Ok(new Ping
			{
				AssemblyVersion = Assembly.GetEntryAssembly().GetName().Version,
				TimestampUtc = DateTime.UtcNow.Ticks,
				ApplicationName = _hostingEnvironment.ApplicationName,
				Environment = _hostingEnvironment.EnvironmentName
			});
		}
	}
}
