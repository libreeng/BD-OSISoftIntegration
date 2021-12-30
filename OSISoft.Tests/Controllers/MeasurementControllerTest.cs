using LibreStream.Api;
using LibreStream.Api.Common.Middleware;
using LibreStream.Api.Iot.Controllers;
using LibreStream.Api.Iot.Services;
using LibreStream.Api.Iot.Tests;
using LibreStream.OSISoft.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace OSISoft.Tests
{
    public class MeasurementControllerTest
	{
		private readonly MeasurementController controller;
		private readonly IMeasurementService service;

		/// <summary>
		/// Constructor
		/// </summary>
		public MeasurementControllerTest()
		{
			service = new IMeasurementServiceTest();
			controller = new MeasurementController(service);
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
										new Claim(SessionTokenAuthenticationHandler.IOT_MEASUREMENT_CLAIM, TestConstants.MEASUREMENT_CLAIM_TEST)
									},
									SessionTokenAuthenticationOptions.DefaultScheme))
				}
			};
		}

		[Fact]
		public void PostQuery_GetEvent_WhenCalled_ReturnsOkResult()
		{
			// arrange
			InitializeController();

			// act
			var result = controller.PostQuery(JsonConvert.DeserializeObject<QueryEventPayload>(TestConstants.IOT_QUERY_GETEVENT_TEST));

			// assert
			Assert.IsType<OkObjectResult>(result.Result);
		}

		[Fact]
		public void PostQuery_Aggregates_WhenCalled_ReturnsOkResult()
		{
			// arrange
			InitializeController();

			// act
			var result = controller.PostQuery(JsonConvert.DeserializeObject<QueryEventPayload>(TestConstants.IOT_QUERY_AGGREGATE_TEST, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				Converters = new List<JsonConverter>
				{
					new Iso8601TimeSpanConverter()
				},
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			}));

			// assert
			Assert.IsType<OkObjectResult>(result.Result);
		}
	}
}
