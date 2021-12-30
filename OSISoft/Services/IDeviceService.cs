using LibreStream.Api.Iot.Models;
using System.Collections.Generic;

namespace LibreStream.Api.Iot.Services
{
    public interface IDeviceService
	{
		/// <summary>
		/// Get a device
		/// </summary>
		/// <param name="connectionString"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		Device GetDevice(IoTDeviceServiceParameters connection, string id);

        /// <summary>
        /// Get a list of devices
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="count">max number of results to return</param>
        /// <returns></returns>
        IEnumerable<Models.Device> GetDevices(IoTDeviceServiceParameters connection, int count);
	}
}
