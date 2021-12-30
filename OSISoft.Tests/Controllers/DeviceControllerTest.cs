using LibreStream.Api;
using LibreStream.Api.Common.Middleware;
using LibreStream.Api.Iot.Controllers;
using LibreStream.Api.Iot.Models;
using LibreStream.Api.Iot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;


namespace OSISoft.Tests
{
    public class DeviceControllerTest
	{

		private readonly DeviceController controller;
		private readonly IDeviceService service;

		/// <summary>
		/// Constructor
		/// </summary>
		public DeviceControllerTest()
		{
			service = new TestDeviceService();
			controller = new DeviceController(service);
		}

		/// <summary>
		/// Initialize the test controller
		/// </summary>
		private void InitializeController()
		{
			controller.ControllerContext = new ControllerContext
			{
				HttpContext = new DefaultHttpContext()
				{
					User = new ClaimsPrincipal(
								new ClaimsIdentity(
									new List<Claim>()
									{
										new Claim(SessionTokenAuthenticationHandler.IOT_DEVICE_CLAIM, TestConstants.DEVICE_CLAIM_TEST)
									},
									SessionTokenAuthenticationOptions.DefaultScheme))
				}
			};
		}

		[Fact]
		public void Get_WhenCalled_ReturnsOkResult()
		{
			// arrange
			InitializeController();

			// act
			var result = controller.Get(-1).Result as OkObjectResult;

			// assert
			List<Device> devices = Assert.IsType<List<Device>>(result.Value);
			Assert.Equal(3, devices.Count);
		}

		[Fact]
		public void GetById_WhenCalled_ReturnsOkResult()
		{
			// arrange
			InitializeController();
			string id = "d1";

			// act
			var result = controller.Get(id).Result as OkObjectResult;

			// assert
			Device device = Assert.IsType<Device>(result.Value);
			Assert.Equal(id, device.DeviceId);
		}

		[Fact]
		public void GetById_UnknownIdPassed_ReturnsNotFoundResult()
		{
			// arrange
			InitializeController();
			string id = "d0";

			// act
			var result = controller.Get(id);

			// assert
			Assert.IsType<NotFoundResult>(result.Result);
		}

		[Fact]
		public void GetByCount_WhenCalled_ReturnsOkResult()
		{
			// arrange
			InitializeController();
			int count = 1;

			// act
			var result = controller.Get(count).Result as OkObjectResult;

			// assert
			List<Device> devices = Assert.IsType<List<Device>>(result.Value);
			Assert.Equal(count, devices.Count);
		}
	}
}
