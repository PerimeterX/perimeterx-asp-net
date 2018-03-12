using System;
using System.Net;
using System.Web;

namespace PerimeterX
{
	class PxCommonUtils
	{
		/**
		 * <summary>
		 * Request helper, extracting the ip from the request according to sockerIpHeader or from 
		 * the request socker when sockerIpHeader is absent
		 * </summary>
		 * <param name="context" >HttpContext</param>
		 * <param name="pxConfig">PxConfiguration</param>
		 * <returns>Ip from the request</returns>
		 */
		public static string GetRequestIP(HttpContext context, PxModuleConfigurationSection pxConfig)
		{
			// Get IP from custom header
			string socketIpHeader = pxConfig.SocketIpHeader;
			if (!string.IsNullOrEmpty(socketIpHeader))
			{
				var headerVal = context.Request.Headers[socketIpHeader];
				if (headerVal != null)
				{
					var ips = headerVal.Split(new char[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					IPAddress firstIpAddress;
					if (ips.Length > 0 && IPAddress.TryParse(ips[0], out firstIpAddress))
					{
						return ips[0];
					}
				}
			}
			return context.Request.UserHostAddress;
		}
	}
}
