using System;
using System.Text;

namespace PerimeterX.DataContracts.Cookies
{
	public sealed class PxCookieV1 : IPxCookie
	{
		private DecodedCookieV1 data;
		private ICookieDecoder cookieDecoder;
		private string rawCookie;

		int IPxCookie.Score
		{
			get
			{
				return data.Score.Bot;
			}
		}

		public string BlockAction
		{
			get
			{
				return "c";
			}
		}

		public string Uuid
		{
			get
			{
				return data.Uuid;
			}
		}

		public string Vid
		{
			get
			{
				return data.Vid;
			}
		}

		public string Hmac
		{
			get
			{
				return data.Hmac;
			}
		}

		public double Timestamp
		{
			get
			{
				return data.Time;
			}
		}

		public object DecodedCookie
		{
			get
			{
				return data;
			}
		}

		public PxCookieV1(ICookieDecoder cookieDecoder, string rawCookie)
		{
			this.rawCookie = rawCookie;
			this.cookieDecoder = cookieDecoder;
		}

		public bool IsSecured(string cookieKey, string[] additionalFields)
		{
			var sb = new StringBuilder()
				.Append(data.Time)
				.Append(data.Score.Application)
				.Append(data.Score.Bot)
				.Append(data.Uuid)
				.Append(data.Vid);
			foreach (string field in additionalFields)
			{
				sb.Append(field);
			}
			return PxCookieUtils.IsHMACValid(cookieKey, sb.ToString(), Hmac);
		}

		public bool Deserialize()
		{
			data = PxCookieUtils.Deserialize<DecodedCookieV1>(cookieDecoder, rawCookie);
			return data != null;
		}
	}
}
