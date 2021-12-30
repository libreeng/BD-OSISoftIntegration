using HealthChecks.UI.Client;
using LibreStream.Api.Common;
using LibreStream.Api.Common.Health;
using LibreStream.Api.Common.Middleware;
using LibreStream.Api.Common.Middleware.ResponseHeaders;
using LibreStream.Api.Iot.Services;
using LibreStream.Api.Iot.Services.OSISoft;
using LibreStream.Api.Utility;
using Microsoft.ApplicationInsights.NLogTarget;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace LibreStream.Api.OSISoft
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
        {
            Configuration = configuration;
            HostingEnvironment = hostingEnvironment;
        }

        public IWebHostEnvironment HostingEnvironment { get; }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
			Utils.Initialize(); // Initializes the security.
			services.AddMemoryCache();
			services.AddHttpClient();
			services.AddSingleton<ParameterCache>();
			services.AddScoped<IDeviceService, OSISoftDeviceService>();
			services.AddScoped<IMeasurementService, OSISoftTsiPreviewService>();

            // add authentication middleware to authenticate by session token
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = SessionTokenAuthenticationOptions.DefaultScheme;
                options.DefaultChallengeScheme = SessionTokenAuthenticationOptions.DefaultScheme;
            })
            .AddSessionTokenAuthentication(options => { });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("HasIoTDeviceParams", policy => policy.RequireClaim(SessionTokenAuthenticationHandler.IOT_DEVICE_CLAIM));
                options.AddPolicy("HasIoTMeasurementParams", policy => policy.RequireClaim(SessionTokenAuthenticationHandler.IOT_MEASUREMENT_CLAIM));

            });

            services.AddControllers();
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApplication2", Version = "v1" });
			});

			services.AddHttpsRedirection(options =>
			{
				options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
				if (!HostingEnvironment.IsDevelopment())
				{
					options.HttpsPort = 443;
				}
			});

			services.AddHsts(options =>
			{
				options.MaxAge = TimeSpan.FromDays(365);
			});

			services.AddMvc().AddNewtonsoftJson(options =>
			{
				options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
				options.SerializerSettings.Converters = new List<JsonConverter>
				{
					new Iso8601TimeSpanConverter()
				};
				options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
			});

			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				Converters = new List<JsonConverter>
				{
					new Iso8601TimeSpanConverter()
				},
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			};

			services.AddHealthChecks()
					.AddCheck("self", () => HealthCheckResult.Healthy())
					.AddUriHealthChecks(Configuration.GetSection("UriHealthChecks").Get<List<UriHealthCheckOptions>>());

			services.AddApplicationInsightsTelemetry();

			ConfigureApplicationInsightsTarget();
		}


		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();

				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApplication2 v1"));
			}
			else
			{
				app.UseHsts();
			}

			app.UseStaticFiles();

			// Add middleware to set proper response headers. This is to satisfy OWASP
			// REST API best practices (OWASP Zap tool and securityheaders.com scanning)
			app.UseOWASPHeaders();

			app.UseHttpsRedirection();

			app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            var healthResultCodes = new Dictionary<HealthStatus, int>()
			{
				[HealthStatus.Healthy] = StatusCodes.Status200OK,
				[HealthStatus.Degraded] = StatusCodes.Status200OK,
				[HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
			};
			app.UseHealthChecks("/api/health", new HealthCheckOptions()
			{
				ResultStatusCodes = healthResultCodes,
				ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
			});
			app.UseHealthChecks("/api/liveness", new HealthCheckOptions
			{
				Predicate = registration => registration.Name.Equals("self"),
				ResultStatusCodes = healthResultCodes,
				ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
			});

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}


		/// <summary>
		/// Configure NLog to write to Applications Insights
		/// </summary>
		public void ConfigureApplicationInsightsTarget()
		{
			// configure NLog
			var config = new NLog.Config.LoggingConfiguration();
			var layout = "${longdate:universalTime=true} ${level:uppercase=true} - ${logger}: ${message} ${exception:format=tostring:innerFormat=tostring:maxInnerExceptionLevel=1000}";
			var logLevel = NLog.LogLevel.Info;
#if DEBUG
			logLevel = NLog.LogLevel.Debug;
#endif
			// add console logging target
			config.LoggingRules.Add(new NLog.Config.LoggingRule("*", logLevel, new NLog.Targets.ConsoleTarget()
			{
				Name = "Console",
				Layout = layout
			}));

			// configure local logging
			string logFileName = "api_cs-iot_azure.log";
			config.LoggingRules.Add(new NLog.Config.LoggingRule("*", logLevel, new NLog.Targets.FileTarget()
			{
				FileName = logFileName,
				Name = "LogFile",
				Layout = layout
			}));

			var aiInstrumentationKey = Configuration["ApplicationInsights:InstrumentationKey"];
			// configure Application Insights logging target
			config.LoggingRules.Add(new NLog.Config.LoggingRule("*", logLevel, new ApplicationInsightsTarget()
			{
				Name = "AppInsights",
				Layout = layout,
				InstrumentationKey = aiInstrumentationKey
			}));

			// set the NLog configuration
			NLog.LogManager.Configuration = config;
		}
	}
}
