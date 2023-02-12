using System;
using System.Collections;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace PerimeterX
{
	class PxCommonUtils
	{
		/**
		 * <summary>
		 * Request helper, extracting the ip from the request according to socketIpHeader or from 
		 * the request socket when socketIpHeader is absent
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

		public static void AddHeaderToRequest(HttpContext context, string key, string value)
		{
			if (HttpRuntime.UsingIntegratedPipeline)
			{
				context.Request.Headers.Add(key, value);
			}
			else
			{
				var headers = context.Request.Headers;
				Type hdr = headers.GetType();
				PropertyInfo ro = hdr.GetProperty("IsReadOnly",
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
				// Remove the ReadOnly property
				ro.SetValue(headers, false, null);
				// Invoke the protected InvalidateCachedArrays method 
				hdr.InvokeMember("InvalidateCachedArrays",
					BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
					null, headers, null);
				// Now invoke the protected "BaseAdd" method of the base class to add the
				// headers you need. The header content needs to be an ArrayList or the
				// the web application will choke on it.
				hdr.InvokeMember("BaseAdd",
					BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
					null, headers,
					new object[] { key, value });
				// repeat BaseAdd invocation for any other headers to be added
				// Then set the collection back to ReadOnly
				ro.SetValue(headers, true, null);
			}
		}

		public static bool IsEmailAddress(string str)
		{
			return Regex.IsMatch(str, PxConstants.EMAIL_ADDRESS_REGEX, RegexOptions.IgnoreCase);
        }

		public static string Sha256(string str)
		{
			using (SHA256 sha256 = SHA256.Create())
			{
				byte[] inputBytes = Encoding.UTF8.GetBytes(str);
				byte[] hash = sha256.ComputeHash(inputBytes);
				return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
			}
		}

		public static string ExtractValueFromNestedJson(string pathToValue, dynamic jsonObject)
		{
            const char CREDENTIAL_FIELD_NESTING_DELIMITER = '.';
			string[] stepsToCredentialInBody = pathToValue.Split(CREDENTIAL_FIELD_NESTING_DELIMITER);

            dynamic result = jsonObject;

            foreach (string step in stepsToCredentialInBody)
            {
                if (result == null || !result.ContainsKey(step))
                {
                    result = null;
                    break;
                }
                else
                {
                    result = result[step];
                }
            }

            return result;
        }
	}
}
