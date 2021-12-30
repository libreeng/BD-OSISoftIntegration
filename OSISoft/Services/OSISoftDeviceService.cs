using LibreStream.Api.Common;
using LibreStream.Api.Iot.Models;
using LibreStream.OSISoft.Models;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LibreStream.Api.Iot.Services.OSISoft
{
    public class OSISoftDeviceService : IDeviceService
	{

		private readonly IMemoryCache cache;

		private readonly IHttpClientFactory clientFactory;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache">Holds the cache</param>
		/// <param name="clientFactory">This will be used to generate HTTP clients</param>
		/// <returns></returns>
		public OSISoftDeviceService(ParameterCache cache, IHttpClientFactory clientFactory)
		{
			this.cache = cache.Cache;
			this.clientFactory = clientFactory;
		}


		/// <summary>
		/// Get a list of devices
		/// </summary>
		/// <param name="connectionInfo"></param>
		/// <param name="count">max number of results to return</param>
		/// <returns></returns>
		public IEnumerable<Models.Device> GetDevices(IoTDeviceServiceParameters connection, int count)
		{
            if (connection.Properties.GetValue("connectionString") == null)
			{
				throw new ArgumentException("connectionString cannot be null or empty");
			}

			return (IEnumerable<Models.Device>)GetDevicesHelper(connection, count).Result;

		}

		/// <summary>
		/// Does all the heavy lifting to get the devices
		/// </summary>
		/// <param name="connectionInfo"></param>
		/// <param name="count">max number of results to return</param>
		/// <returns></returns>
		private async Task<IEnumerable<Models.Device>> GetDevicesHelper(IoTDeviceServiceParameters connection, int count)
		{
			// create a HTTP Client and fetch all URL that would assist in getting all the devices in the facility
			HttpClient client = clientFactory.CreateClient();
			OSISoftElement allDevicesInFacility = null;
			string authInfo = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(String.Format("{0}:{1}", connection.Properties.GetValue("username"), connection.Properties.GetValue("password"))));
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authInfo);
			HttpResponseMessage theResponse = await client.GetAsync(connection.Properties.GetValue("connectionString").ToString());
			if (theResponse.IsSuccessStatusCode) // facility found
			{
				string contentString = await theResponse.Content.ReadAsStringAsync();
				allDevicesInFacility = JsonConvert.DeserializeObject<OSISoftElement>(contentString);
			}

			// if the facility is found
			if (allDevicesInFacility != null)
			{
				// devices represented as elements in the facility
				string getAllDevices = allDevicesInFacility.Links.Elements;
				// if a count is specified
				if (count != -1)
				{
					getAllDevices += "?maxCount=" + count;
				}

				// we have the url to fetch all the devices, hence we will make a request
				OSISoftDevices osiSoftDevices = null;
				using (HttpResponseMessage response = await client.GetAsync(getAllDevices))
				{
					if (response.IsSuccessStatusCode) // facility found
					{
						string contentString = await response.Content.ReadAsStringAsync();
						osiSoftDevices = JsonConvert.DeserializeObject<OSISoftDevices>(contentString);
					}
				}

				// convert OSISoft to Azure
				return await ConvertOSISoftDevicesToStandard(osiSoftDevices, connection);
			}
			else  // facility not found
			{
				throw new Exception("An Error occured while making a connection with OSISoft using the connection string" + connection.Properties.GetValue("connectionString").ToString());
			}
		}

		/// <summary>
		/// Takes OSISoft Devices and returns devices similar to Azure devices
		/// </summary>
		/// <param name="osiSoftDevices"></param>
		/// <param name="connectionInfo"></param>
		/// <returns></returns>
		private async Task<IEnumerable<Models.Device>> ConvertOSISoftDevicesToStandard(OSISoftDevices osiSoftDevices, IoTDeviceServiceParameters connection)
		{
			List<Models.Device> devices = new List<Models.Device>();

			// for every device	
			for (int currentDevice = 0; currentDevice < osiSoftDevices.Items.Count; currentDevice++)
			{
				// get the OSISoft device and initialize a new Azure Device model
				OSISoftElement osiSoftDevice = osiSoftDevices.Items[currentDevice];
				Models.Device device = new Models.Device();
				OSISoftElementSummary summary = null;

				// create a HTTP Client to fetch the summary data for the device
				// summary data of the device helps to pinpoint limits (min and max values) associated with any attributes of the device
				var client = clientFactory.CreateClient();
				string authInfo = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(String.Format("{0}:{1}", connection.Properties.GetValue("username"), connection.Properties.GetValue("password"))));
				client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authInfo);
				using (HttpResponseMessage response = await client.GetAsync(osiSoftDevice.Links.SummaryData))
				{
					if (response.IsSuccessStatusCode) // summary for device found
					{
						string contentString = await response.Content.ReadAsStringAsync();
						summary = JsonConvert.DeserializeObject<OSISoftElementSummary>(contentString);
					}
				}

				// if we have the summary data of the device, then proceed, otherwise throw an exception
				if (summary != null)
				{
					OSISoftAttributes attributes = null;
					// this HTTP call fetches all the attribute data in details which includes units, last updated etc. 
					using (HttpResponseMessage response = await client.GetAsync(osiSoftDevice.Links.Attributes))
					{
						if (response.IsSuccessStatusCode)
						{
							string contentString = await response.Content.ReadAsStringAsync();
							attributes = JsonConvert.DeserializeObject<OSISoftAttributes>(contentString);
						}
					}

					// if we have all the attributes
					if (attributes != null)
					{
						// initialize some variables for the return object
						JObject tags = new JObject();
						JObject properties = new JObject();
						JObject messageTemplate = new JObject();
						OSISoftAttributes childAttribute = null;
						DateTime lastActivityTime = DateTime.MinValue;
						JObject allActivitiesTime = new JObject();
						JObject allAttributeValues = new JObject();
                        properties["reported"] = new JObject
                        {
                            ["Telemetry"] = new JObject(),
                            ["$metadata"] = new JObject()
                        };
                        properties["reported"]["Telemetry"][osiSoftDevice.Name] = new JObject
                        {
                            ["MessageSchema"] = new JObject()
                        };
                        properties["reported"]["Telemetry"][osiSoftDevice.Name]["MessageSchema"]["Name"] = osiSoftDevice.Name;
						properties["reported"]["Telemetry"][osiSoftDevice.Name]["MessageSchema"]["Format"] = "JSON";
						properties["reported"]["Telemetry"][osiSoftDevice.Name]["MessageSchema"]["Fields"] = new JObject();
                        properties["reported"]["$metadata"]["Telemetry"] = new JObject
                        {
                            [osiSoftDevice.Name] = new JObject()
                        };
                        properties["reported"]["$metadata"]["Telemetry"][osiSoftDevice.Name]["MessageSchema"] = new JObject
                        {
                            ["Fields"] = new JObject()
                        };

                        // this loop parses and collects the last activity timestamps for each of the attributes
                        for (int currActivity = 0; currActivity < summary.Items.Count; currActivity++)
						{
							DateTime time = DateTime.Parse((string)summary.Items[currActivity].Items[0]["Value"]["Timestamp"]);
							if (time > lastActivityTime)
							{
								lastActivityTime = time;

							}
							allActivitiesTime[summary.Items[currActivity].Name] = summary.Items[currActivity].Items[0]["Value"]["Timestamp"];
							allAttributeValues[summary.Items[currActivity].Name] = summary.Items[currActivity].Items[0]["Value"]["Value"];
						}

						// this loop translates all the attributes from OSISoft to Azure
						for (int currentAttribute = 0; currentAttribute < attributes.Items.Count; currentAttribute++)
						{
							// get child attributes of current attribute, the child attributes hold any tags e.g max, max-critical etc. 
							using (HttpResponseMessage response = await client.GetAsync(attributes.Items[currentAttribute].Links.Attributes))
							{
								if (response.IsSuccessStatusCode)
								{
									string contentString = await response.Content.ReadAsStringAsync();
									childAttribute = JsonConvert.DeserializeObject<OSISoftAttributes>(contentString);
								}
							}

							if (childAttribute != null && childAttribute.Items.Count != 0)
							{
								for (int currentChildAttribute = 0; currentChildAttribute < childAttribute.Items.Count; currentChildAttribute++)
								{
									string tagKeyName = childAttribute.Items[currentChildAttribute].Name switch
									{
										"Maximum" => "max",
										"Minimum" => "min",
										"LoLo" => "min_critical",
										"HiHi" => "max_critical",
										"Lo" => "min_warning",
										"Hi" => "max_warning",
										_ => null
									};

									if (tagKeyName != null)
									{
										JObject value = null;

										// get the max, min, max-warning etc. value
										using (HttpResponseMessage response = await client.GetAsync(childAttribute.Items[currentChildAttribute].Links.Value))
										{
											if (response.IsSuccessStatusCode)
											{
												string contentString = await response.Content.ReadAsStringAsync();
												value = JsonConvert.DeserializeObject<JObject>(contentString);
											}
										}

										if (value != null)
										{
											// ensure measurement units exist for  the attribute
											if (attributes.Items[currentAttribute].Name.Length != 0)
											{
												if (tags[attributes.Items[currentAttribute].Name] != null)
												{
													tags[attributes.Items[currentAttribute].Name][tagKeyName] = value["Value"];
												}
												else
												{
													JObject tagItem = new JObject
													{
														{ tagKeyName,  value["Value"] }
													};
													tags[attributes.Items[currentAttribute].Name] = tagItem;
												}
											}
										}
									}
								}
								
							}

							// store GPS coordinates, this is a special case
							if (attributes.Items[currentAttribute].Name == "Latitude" || attributes.Items[currentAttribute].Name == "Longitude")
							{

								properties["reported"][attributes.Items[currentAttribute].Name] = allAttributeValues[attributes.Items[currentAttribute].Name];
								properties["reported"]["$metadata"][attributes.Items[currentAttribute].Name] = new JObject
								{
									{ "$lastUpdated",  allActivitiesTime[attributes.Items[currentAttribute].Name] }
								};

							}
							else if (attributes.Items[currentAttribute].Name == "Health Status") // this is a special case
							{
								JObject currHealthValue = null;
								// get the max, min, max-warning etc. value
								using (HttpResponseMessage response = await client.GetAsync(attributes.Items[currentAttribute].Links.Value))
								{
									if (response.IsSuccessStatusCode)
									{
										string contentString = await response.Content.ReadAsStringAsync();
										currHealthValue = JsonConvert.DeserializeObject<JObject>(contentString);
									}
								}

								if (currHealthValue != null)
								{
									device.Status = currHealthValue["Value"]["Name"].ToString();
									device.StatusUpdateTime = DateTime.Parse(allActivitiesTime[attributes.Items[currentAttribute].Name].ToString());
								}
							}
							else // if the current attribute is a normal attribute
							{
								properties["reported"]["Telemetry"][osiSoftDevice.Name]["MessageSchema"]["Fields"][attributes.Items[currentAttribute].Name] = attributes.Items[currentAttribute].Type;
								properties["reported"]["Telemetry"][osiSoftDevice.Name]["MessageSchema"]["Fields"][attributes.Items[currentAttribute].Name + "_unit"] = "Text";
								properties["reported"]["$metadata"]["Telemetry"][osiSoftDevice.Name]["MessageSchema"]["Fields"][attributes.Items[currentAttribute].Name] = new JObject
								{
									{ "$lastUpdated",  allActivitiesTime[attributes.Items[currentAttribute].Name] }
								};
								messageTemplate[attributes.Items[currentAttribute].Name] = "${" + attributes.Items[currentAttribute].Name + "}";
								messageTemplate[attributes.Items[currentAttribute].Name + "_unit"] = "${" + attributes.Items[currentAttribute].Name + "_unit}";
							}

						}

						// record the extended properties if any
						if (osiSoftDevice.ExtendedProperties != null)
						{
							foreach (var extendedProperty in osiSoftDevice.ExtendedProperties)
							{
								properties["reported"][extendedProperty.Key] = ((JObject)extendedProperty.Value)["Value"];
							}
						}

						// match other fields
						properties["reported"]["Telemetry"][osiSoftDevice.Name]["MessageTemplate"] = messageTemplate.ToString();
						device.LastActivityTime = lastActivityTime;
						device.DeviceId = osiSoftDevice.WebId;
						device.Properties = properties;
						device.Tags = tags;
						// add the device
						devices.Add(device);
					}
				}
				else
                {
					throw new Exception("An Error occured while retrieving the summary of a device from OSISoft. Device:" + osiSoftDevices.Items[currentDevice].ToString());
				}
			}

			return devices;
		}


		/// <summary>
		/// Get a device
		/// </summary>
		/// <param name="connectionInfo"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public Models.Device GetDevice(IoTDeviceServiceParameters connection, string id)
		{

			if (connection.Properties.GetValue("connectionString") == null)
			{
				throw new ArgumentException("connectionString cannot be null or empty");
			}

			string url = connection.Properties.GetValue("connectionString") + "/" + id;

			IEnumerable<Models.Device> devices = GetDeviceHelper(url, connection).Result;

			if (devices !=null && devices.Count() != 0)
			{
				return devices.First();
			}
			else
			{
				return null;
			}

		}


		/// <summary>
		/// Does all the heavy lifting to get the device
		/// </summary>
		/// <param name="connectionString"></param>
		/// <param name="id">The ID of the device to fetch</param>
		/// <returns></returns>
		private async Task<IEnumerable<Models.Device>> GetDeviceHelper(string url, IoTDeviceServiceParameters connection)
		{
			// create a HTTP Client
			HttpClient client = clientFactory.CreateClient();
			OSISoftElement device = null;

			string authInfo = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(String.Format("{0}:{1}", connection.Properties.GetValue("username"), connection.Properties.GetValue("password"))));
			client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authInfo);
			HttpResponseMessage theResponse = await client.GetAsync(url);
			if (theResponse.IsSuccessStatusCode) // facility found
			{
				string contentString = await theResponse.Content.ReadAsStringAsync();
				device = JsonConvert.DeserializeObject<OSISoftElement>(contentString);
			}

			if (device != null)
			{
                OSISoftDevices oSISoftDevices = new OSISoftDevices
                {
                    Items = new List<OSISoftElement>
					{
						device
					}
                };

                return ConvertOSISoftDevicesToStandard(oSISoftDevices, connection).Result;
			}
			else  // facility not found
			{
				return null;
			}
		}
	}
}
