using Jil;
using PerimeterX.DataContracts.Cookies;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PerimeterX
{
	public static class PxCookieUtils
	{
		public static IPxCookie BuildCookie(PxModuleConfigurationSection config, Dictionary<string,string> cookies, ICookieDecoder cookieDecoder)
		{
			if (cookies.Count == 0)
			{
				return null;
			}

			if (cookies.ContainsKey(PxConstants.COOKIE_V3_PREFIX))
			{
				return new PxCookieV3(cookieDecoder, cookies[PxConstants.COOKIE_V3_PREFIX]);
			}
			
			return new PxCookieV1(cookieDecoder, cookies[PxConstants.COOKIE_V1_PREFIX]);
			
		}

		public static T Deserialize<T>(ICookieDecoder cookieDecoder, string rawCookie)
		{
			string cookieString = cookieDecoder.Decode(rawCookie);
			if (string.IsNullOrEmpty(cookieString))
			{
				return default(T);
			}

			return JSON.Deserialize<T>(cookieString, PxConstants.JSON_OPTIONS);
		}

		public static bool IsExpired(double date)
		{
			double now = DateTime.UtcNow
							.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
							.TotalMilliseconds;
			return date < now;
		}

		public static bool IsHMACValid(string cookieKey, string UncodedHmac, string CookieHmac)
		{
			var cookieKeyBytes = Encoding.UTF8.GetBytes(cookieKey);
			var hash = new HMACSHA256(cookieKeyBytes);
			var expectedHashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(UncodedHmac));
			var encodedHmac = ByteArrayToHexString(expectedHashBytes);
			return encodedHmac == CookieHmac;
		}

		public static string ByteArrayToHexString(byte[] input)
		{
			StringBuilder sb = new StringBuilder(input.Length * 2);
			foreach (byte b in input)
			{
				sb.Append(PxConstants.HEX_ALPHABET[b >> 4]);
				sb.Append(PxConstants.HEX_ALPHABET[b & 0xF]);
			}
			return sb.ToString();
		}
	}

}
