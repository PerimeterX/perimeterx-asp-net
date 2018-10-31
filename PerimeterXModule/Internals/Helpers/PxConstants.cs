using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using Jil;


namespace PerimeterX
{
	public static class PxConstants
	{
		public static readonly string HEX_ALPHABET = "0123456789abcdef";
		public static readonly string[] PX_COOKIES_PREFIX = { COOKIE_V1_PREFIX, COOKIE_V3_PREFIX };
		public static readonly string[] PX_TOKEN_PREFIX = { TOKEN_V1_PREFIX, TOKEN_V3_PREFIX };
		public const string COOKIE_V1_PREFIX = "_px";
		public const string COOKIE_V3_PREFIX = "_px3";
		public const string TOKEN_V1_PREFIX = "1";
		public const string TOKEN_V3_PREFIX = "3";
		public static readonly string PX_VALIDATED_HEADER = "X-PX-VALIDATED";
		public static readonly string MOBILE_HEADER = "X-PX-AUTHORIZATION";
		public static readonly string ORIGINAL_TOKEN = "X-PX-ORIGINAL-TOKEN";
		public static readonly string FORWARDED_FOR_HEADER = "x-forwarded-for";
		public static readonly string CONFIG_SECTION = "perimeterX/pxModuleConfigurationSection";
		public static readonly string LOG_CATEGORY = "PxModule";
		public static readonly string MODULE_VERSION = GetAssemblyVersion();
		public static readonly Options JSON_OPTIONS = new Options(prettyPrint: false, excludeNulls: true, includeInherited: true);
		public static readonly string JS_CHALLENGE_ACTION = "j";
		public static readonly string ENFORCER_TRUE_IP_HEADER = "x-px-enforcer-true-ip";
		public static readonly string FIRST_PARTY_HEADER = "X-PX-FIRST-PARTY";
		public static readonly string FIRST_PARTY_VALUE = "1";
		public static readonly string COOKIE_HEADER = "cookie";

		// Endpoints
		public const string RISK_API_V2 = "/api/v2/risk";
		public const string ACTIVITIES_API_PATH = "/api/v1/collector/s2s";
		public const string ENFORCER_TELEMETRY_API_PATH = "/api/v2/risk/telemetry";

		private static string GetAssemblyVersion()
		{
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return "ASP.NET v" + fvi.FileVersion;
		}

		public static string FormatBaseUri(PxModuleConfigurationSection config)
		{
			return string.Format(config.BaseUri, config.AppId);
		}

		internal static HttpClient CreateHttpClient(bool expectContinue = false, int timeout = 5000, bool useAuth = false, PxModuleConfigurationSection config = null)
		{
			var webRequestHandler = new WebRequestHandler
			{
				AllowPipelining = true,
				UseDefaultCredentials = true,
				UnsafeAuthenticatedConnectionSharing = true
			};

			HttpClient httpClient = new HttpClient(webRequestHandler, true) { Timeout = TimeSpan.FromMilliseconds(timeout) };
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			httpClient.DefaultRequestHeaders.ExpectContinue = expectContinue;

			if (useAuth)
			{
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiToken);
			}

			return httpClient;
		}

		internal static double GetTimestamp()
		{
			return Math.Round(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, MidpointRounding.AwayFromZero);
		}
	}
}
