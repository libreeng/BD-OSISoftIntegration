using Microsoft.AspNetCore.Mvc;
using System;
using System.Runtime.ExceptionServices;

namespace LibreStream.Api.Common.Controllers
{
    public class OSISoftControllerBase : ControllerBase
	{
		/// <summary>
		/// Handles exceptions, re-throws those that are unknown
		/// </summary>
		/// <param name="ex"></param>
		/// <returns></returns>
		protected ActionResult HandleException(Exception ex)
		{
			if (ex is AggregateException && ex.InnerException != null)
			{
				return HandleException(ex.InnerException);
			}
			else if (ex is System.Net.WebException webException && webException.Response is System.Net.HttpWebResponse response)
			{
				return StatusCode((int)response.StatusCode, webException.Message);
			}

			// rethrow anything we don't understand
			ExceptionDispatchInfo.Capture(ex).Throw();  // this line preserves the stack trace
			throw ex;
		}
	}
}
