using LibreStream.Api.Iot.Models;
using LibreStream.Api.Iot.Services;
using System.Collections.Generic;
using System.Linq;

namespace OSISoft.Tests
{
    class IDeviceServiceTest : IDeviceService
	{
        readonly List<Device> devices = new List<Device>();

		/// <summary>
		/// Constructor
		/// </summary>
		public IDeviceServiceTest()
		{
			devices.Add(new Device()
			{
				DeviceId = "d1",
			});
			devices.Add(new Device()
			{
				DeviceId = "d2",
			});
			devices.Add(new Device()
			{
				DeviceId = "d3",
			});
		}

		/// <summary>
		/// Get device
		/// </summary>
		/// <param name="connectionString"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public Device GetDevice(IoTDeviceServiceParameters connection, string id)
		{
			try
			{
				return devices.Single(p => p.DeviceId == id);
			}
			catch (System.InvalidOperationException)
			{
				return null;
			}
		}

		/// <summary>
		/// Get a list of devices
		/// </summary>
		/// <param name="connectionString"></param>
		/// <param name="count"></param>
		/// <returns></returns>
		public IEnumerable<Device> GetDevices(IoTDeviceServiceParameters connection, int count)
		{
			return count > -1 ? devices.Take<Device>(count).ToList<Device>() : devices;
		}
	}
}
