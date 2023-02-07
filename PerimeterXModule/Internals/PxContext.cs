using System.Collections.Generic;
using System.Web;
using System;
using System.Collections.Specialized;
using System.Linq;
using PerimeterX.DataContracts.Cookies;
using PerimeterX.Internals.CredentialsIntelligence;

namespace PerimeterX
{
	public class PxContext
	{
		readonly char[] MOBILE_DELIMITER = new char[] { ':' };

		public Dictionary<string, string> PxCookies { get; set; }
		public object DecodedPxCookie { get; set; }
		public string PxCookieHmac { get; set; }
		public string Ip { get; set; }
		public string HttpVersion { get; set; }
		public string HttpMethod { get; set; }
		public List<RiskRequestHeader> Headers { get; set; }
		public string Hostname { get; set; }
		public string Uri { get; set; }
		public string UserAgent { get; set; }
		public string FullUrl { get; set; }
		public string S2SCallReason { get; set; }
		public int Score { get; set; }
		public string Vid { get; set; }
		public string UUID { get; set; }
		public BlockReasonEnum BlockReason { get; set; }
		public bool MadeS2SCallReason { get; set; }
		public string S2SHttpErrorMessage { get; set; }
		public string BlockAction { get; set; }
		public string BlockData { get; set; }
		public HttpContext ApplicationContext { get; private set; }
		public bool SensitiveRoute { get; set; }
		public PassReasonEnum PassReason { get; set; }
		public long RiskRoundtripTime { get; set; }
		public CookieOrigin CookieOrigin { get; set; }
		public Dictionary<string,string> OriginalTokens { get; set; }
		public IPxCookie OriginalToken { get; set; }
		public string OriginalTokenError { get; set; }
		public string OriginalUUID { get; set; }
		public object DecodedOriginalToken { get; set; }
		public bool IsMobileRequest { get; set; }
		public string MobileHeader { get; set; }
		public string[] CookieNames;
		public bool IsPxdeVerified { get; set; }
		public dynamic Pxde { get; set; }
		public string CustomBlockUrl { get;  set; }
		public bool RedirectOnCustomUrl { get; set; }
		public string VidSource { get; set; }
		public string Pxhd { get; set; }
        public bool MonitorRequest { get; set; }
		public LoginCredentialsFields LoginCredentialsFields { get; set; }

        public PxContext(HttpContext context, PxModuleConfigurationSection pxConfiguration)
		{
			ApplicationContext = context;

			CookieOrigin = CookieOrigin.COOKIE;
			PxCookies = new Dictionary<string, string>();
			OriginalTokens = new Dictionary<string, string>();
			S2SCallReason = "none";
			IsMobileRequest = false;

			// Get Headers

			// if userAgentOverride is present override the default user-agent
			CookieNames = extractCookieNames(context.Request.Headers[PxConstants.COOKIE_HEADER]);
			string userAgentOverride = pxConfiguration.UserAgentOverride;
			if (!string.IsNullOrEmpty(userAgentOverride))
			{
				UserAgent = context.Request.Headers[userAgentOverride];
			}
			// ua fallback
			if (string.IsNullOrEmpty(UserAgent))
			{
				UserAgent = context.Request.Headers["user-agent"];
			}

			Headers = new List<RiskRequestHeader>();

			foreach (string header in context.Request.Headers.Keys)
			{
				if (!pxConfiguration.SensitiveHeaders.Contains(header))
				{
					RiskRequestHeader riskHeader = new RiskRequestHeader
					{
						Name = header,
						Value = context.Request.Headers.Get(header)
					};
					if (header.ToLower() == "user-agent")
					{
						riskHeader.Value = UserAgent;
					}
					Headers.Add(riskHeader);
				}
			}

			// Handle Cookies
			var contextCookie = context.Request.Cookies;
			string mobileHeader = context.Request.Headers[PxConstants.MOBILE_HEADER];
			// Check if X-PX-AUTHORIZATION exist
			if (mobileHeader != null)
			{
				MobileHeader = mobileHeader;
				// Extract Original Tokens
				CookieOrigin = CookieOrigin.HEADER;
				IsMobileRequest = true;

				// Extact Token
				string[] splittedToken = mobileHeader.Split(MOBILE_DELIMITER, 2, StringSplitOptions.RemoveEmptyEntries);
				if (splittedToken.Length > 1 && Array.IndexOf(PxConstants.PX_TOKEN_PREFIX, splittedToken[0]) > -1)
				{
					string tokenKey = splittedToken[0].Equals(PxConstants.TOKEN_V3_PREFIX) ? PxConstants.COOKIE_V3_PREFIX : PxConstants.COOKIE_V1_PREFIX;
					PxCookies.Add(tokenKey, splittedToken[1]);
				}
				else
				{
					PxCookies.Add(PxConstants.COOKIE_V3_PREFIX, mobileHeader);
				}

				string originalToken = context.Request.Headers[PxConstants.ORIGINAL_TOKEN];
				if (!string.IsNullOrEmpty(originalToken))
				{
					// Extract original token
					string[] splittedOriginalToken = originalToken.Split(MOBILE_DELIMITER, 2, StringSplitOptions.RemoveEmptyEntries);
					string[] fallbackSplittedOriginalToken = originalToken.Split(MOBILE_DELIMITER);
					if (splittedOriginalToken.Length > 1 && Array.IndexOf(PxConstants.PX_TOKEN_PREFIX, splittedOriginalToken[0]) > -1)
					{
						string originlTokenKey = splittedOriginalToken[0].Equals(PxConstants.TOKEN_V3_PREFIX) ? PxConstants.COOKIE_V3_PREFIX : PxConstants.COOKIE_V1_PREFIX;
						OriginalTokens.Add(originlTokenKey, splittedOriginalToken[1]);
					}
					// Fallback
					else if (fallbackSplittedOriginalToken.Length == 3)
					{
						OriginalTokens.Add(PxConstants.COOKIE_V1_PREFIX, originalToken);
					}
					else
					{
						OriginalTokens.Add(PxConstants.COOKIE_V3_PREFIX, originalToken);
					}
				}
			}
			else
			{
				// Case its not mobile token
				foreach (string key in contextCookie.AllKeys)
				{
					if (Array.IndexOf(PxConstants.PX_COOKIES_PREFIX, key) > -1)
					{
						PxCookies[key] = contextCookie.Get(key).Value;
					}
				}

				DataEnrichmentCookie deCookie = PxCookieUtils.GetDataEnrichmentCookie(PxCookies, pxConfiguration.CookieKey);
				IsPxdeVerified = deCookie.IsValid;
				Pxde = deCookie.JsonPayload;
				if (PxCookies.ContainsKey(PxConstants.COOKIE_VID_PREFIX))
				{
					Vid = PxCookies[PxConstants.COOKIE_VID_PREFIX];
					VidSource = PxConstants.VID_COOKIE;
				}
				if (PxCookies.ContainsKey("_" + PxConstants.COOKIE_VID_PREFIX))
				{
					Vid = PxCookies["_" + PxConstants.COOKIE_VID_PREFIX];
					VidSource = PxConstants.VID_COOKIE;
				}
				if (PxCookies.ContainsKey(PxConstants.COOKIE_PXHD_PREFIX))
				{
					Pxhd = PxCookies[PxConstants.COOKIE_PXHD_PREFIX];
				}
			}

			Hostname = context.Request.Url.Host;

			Uri = context.Request.Url.PathAndQuery;
			FullUrl = context.Request.Url.ToString();
			Score = 0;
			RiskRoundtripTime = 0;
			BlockReason = BlockReasonEnum.NONE;
			PassReason = PassReasonEnum.NONE;

			Ip = PxCommonUtils.GetRequestIP(context, pxConfiguration);

			HttpVersion = ExtractHttpVersion(context);
			HttpMethod = context.Request.HttpMethod;

			SensitiveRoute = CheckSensitiveRoute(pxConfiguration.SensitiveRoutes, Uri);

			CustomBlockUrl = pxConfiguration.CustomBlockUrl;
			RedirectOnCustomUrl = pxConfiguration.RedirectOnCustomUrl;

            MonitorRequest = shouldMonitorRequest(context, pxConfiguration);
		}

        private bool shouldMonitorRequest(HttpContext context, PxModuleConfigurationSection pxConfiguration)
        {
            string uri = context.Request.Url.AbsolutePath;
            if (uri.IndexOf("/", StringComparison.Ordinal) == 0)
            {
                uri = uri.Substring(1);
            }

            var mitigationUrls = pxConfiguration.MitigationUrls;
            if (mitigationUrls.Count > 0)
            {
                return !mitigationUrls.Contains(uri);
            }

            if (!string.IsNullOrEmpty(pxConfiguration.ByPassMonitorHeader))
            {
                if (context.Request.Headers[pxConfiguration.ByPassMonitorHeader] == "1")
                {
                    return false;
                }
            }

            return pxConfiguration.MonitorMode;
        }

        private string[] extractCookieNames(string cookieHeader)
		{
			string[] cookieNames =  null;
			if (cookieHeader != null)
			{
				var cookies = cookieHeader.Split(';');
				cookieNames = new string[cookies.Length];
				for (int i = 0; i < cookies.Length; i++)
				{
					cookieNames[i] = cookies[i].Split('=')[0].Trim();
				}
			}
			return cookieNames;
		}

		private bool CheckSensitiveRoute(StringCollection sensitiveRoutes, string uri)
		{
			if (sensitiveRoutes != null)
			{
				foreach (string sensitiveRoute in sensitiveRoutes)
				{
					if (uri.StartsWith(sensitiveRoute))
					{
						return true;
					}
				}
			}

			return false;
		}

		private string ExtractHttpVersion(HttpContext context)
		{
			string serverProtocol = context.Request.ServerVariables["SERVER_PROTOCOL"];
			if (serverProtocol != null)
			{
				int i = serverProtocol.IndexOf("/");
				if (i != -1)
				{
					return serverProtocol.Substring(i + 1);
				}
			}
			return serverProtocol;
		}

		public string GetPxCookie()
		{
			if (PxCookies.Count == 0)
			{
				return null;
			}
			return PxCookies.ContainsKey(PxConstants.COOKIE_V3_PREFIX) ? PxCookies[PxConstants.COOKIE_V3_PREFIX] : PxCookies[PxConstants.COOKIE_V1_PREFIX];
		}

		public Dictionary<string, string> GetHeadersAsDictionary()
		{
			Dictionary<string, string> headersDictionary = new Dictionary<string, string>();

			if (Headers != null && Headers.Count() > 0)
			{
				headersDictionary = Headers.ToDictionary(header => header.Name, header => header.Value);
			}

			return headersDictionary;
		}

		public string MapBlockAction()
		{
			if (string.IsNullOrEmpty(BlockAction))
			{
				return null;
			}
			
			switch (BlockAction) {
				case "c":
					return "captcha";
				case "b":
					return "block";
				case "j":
					return "challenge";
				default:
					return "captcha";
			}
		}
	}
}
