using LibreStream.Api.Common.Controllers;
using LibreStream.Api.Common.Middleware;
using LibreStream.Api.Iot.Models;
using LibreStream.Api.Iot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LibreStream.Api.Iot.Controllers
{
    [Route("api/iot/[controller]")]
	[ApiController]
	[Consumes("application/json")]
	[Produces("application/json")]
	[ApiExplorerSettings(GroupName = "iot")]
	public class DeviceController : OSISoftControllerBase
	{

		private readonly IDeviceService service;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="service"></param>
		public DeviceController(IDeviceService service)
		{
			this.service = service;
		}

		/// <summary>
		/// Get a list of devices
		/// </summary>
		/// <param name="count">max number of devices to return</param>
		/// <returns></returns>
		[HttpGet]
		[Authorize(Policy = "HasIoTDeviceParams")]
		public ActionResult<IEnumerable<Device>> Get(int count = -1)
		{
			try
			{
				IoTDeviceServiceParameters connection = GetConnectionString();
				if (connection != null && connection.Properties.GetValue("connectionString") != null && connection.Properties.GetValue("username") != null && connection.Properties.GetValue("password") != null)
				{
                    IEnumerable<Device> devices = service.GetDevices(connection, count);

                    if (devices.Count<Device>() > 0)
                    {
                        return Ok(devices);
                    }
                    return NotFound();
                }
				return BadRequest("Connection string is invalid");
			}
			catch (Exception ex)
			{
				return HandleException(ex);
			}
		}

		/// <summary>
		/// Get a device by id
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[HttpGet("{id}", Name = "Get")]
		[Authorize(Policy = "HasIoTDeviceParams")]
		public ActionResult<Device> Get(string id)
		{
			try
			{
				IoTDeviceServiceParameters connection = GetConnectionString();
				if (connection != null && connection.Properties.GetValue("connectionString") != null && connection.Properties.GetValue("username") != null && connection.Properties.GetValue("password") != null)
				{
                    Device device = service.GetDevice(connection, id);
                    if (device != null)
                    {
                        return Ok(device);
                    }
                    return NotFound();
                }
				return BadRequest("Connection string is invalid");
			}
			catch (Exception ex)
			{
				return HandleException(ex);
			}
		}

		/// <summary>
		/// Get the connection string for this service
		/// </summary>
		/// <returns></returns>
		private IoTDeviceServiceParameters GetConnectionString()
		{
			return JsonConvert.DeserializeObject<IoTDeviceServiceParameters>(HttpContext.User.Claims.FirstOrDefault(c => c.Type == SessionTokenAuthenticationHandler.IOT_DEVICE_CLAIM)?.Value);
		}
	}
}
