﻿using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.Serialization;
using Jil;


namespace PerimeterX
{

	public static class PxConstants
	{
		public static readonly string HEX_ALPHABET = "0123456789abcdef";
		public static readonly string[] PX_COOKIES_PREFIX = { COOKIE_V1_PREFIX, COOKIE_V3_PREFIX };
		public const string COOKIE_V1_PREFIX = "_px";
		public const string COOKIE_V3_PREFIX = "_px3";
		public static readonly string COOKIE_CAPTCHA_PREFIX = "_pxCaptcha";
		public static readonly string PX_VALIDATED_HEADER = "X-PX-VALIDATED";
		public static readonly string CONFIG_SECTION = "perimeterX/pxModuleConfigurationSection";
		public static readonly string LOG_CATEGORY = "PxModule";
		public static readonly string MODULE_VERSION = GetAssemblyVersion();
		public static readonly Options JSON_OPTIONS = new Options(prettyPrint: false, excludeNulls: true, includeInherited: true);
        public static readonly string REMOTE_CONFIGURATION_SERVER = "http://px-conf-mgmt-stg.perimeterx.net";
        public static readonly string REMOTE_CONFIGURATION_PATH = "/module";

		// Endpoints
		public const string RISK_API_V2 = "/api/v2/risk";
		public const string CAPTCHA_API_V1 = "/api/v1/risk/captcha";

		private static string GetAssemblyVersion()
		{
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			return "ASP.NET v" + fvi.FileVersion;
		}

		public static string FormatBaseUri(PXConfigurationWrapper pxConfig)
		{
			return string.Format(pxConfig.BaseUri, pxConfig.AppId);
		}

		public static StringCollection ArrayToStringCollection(string[] array)
		{
			StringCollection collection = new StringCollection();
			collection.AddRange(array);
			return collection;
		}
	}
}
