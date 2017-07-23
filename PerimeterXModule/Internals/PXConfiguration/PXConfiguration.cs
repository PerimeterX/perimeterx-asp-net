using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	class PXConfiguration : IPXConfiguration
	{

		public int ActivitiesBulkSize { get; }
		public int ActivitiesCapacity { get; }
		public int ApiTimeout { get; }
		public string ApiToken { get; }
		public string AppId { get; }
		public string BaseUri { get; }
		public int BlockingScore { get; }
		public bool CaptchaEnabled { get; }
		public string CookieKey { get; }
		public string CookieName { get; }
		public string CssRef { get; }
		public string CustomLogo { get; }
		public bool Enabled { get; }
		public bool EncryptionEnabled { get; }
		public StringCollection FileExtWhitelist { get; }
		public string JsRef { get; }
		public bool MonitorMode { get; }
		public bool RemoteConfigurationEnabled { get; }
		public int ReporterApiTimeout { get; }
		public StringCollection RoutesWhitelist { get; }
		public bool SendBlockActivites { get; }
		public bool SendPageActivites { get; }
		public StringCollection SensitiveHeaders { get; }
		public StringCollection SensitiveRoutes { get; }
		public bool SignedWithIP { get; }
		public bool SignedWithUserAgent { get; }
		public StringCollection SocketIpHeaders { get; }
		public bool SuppressContentBlock { get; }
		public string UserAgentOverride { get; }
		public StringCollection UseragentsWhitelist { get; }
		public string RemoteConfigurationUrl { get; }
		public string RemoteConfigurationPath { get; }
		public int RemoteConfigurationInterval { get; }

		public PXConfiguration(PxModuleConfigurationSection config)
		{
			ActivitiesBulkSize = config.ActivitiesBulkSize;
			ActivitiesCapacity = config.ActivitiesCapacity;
			ApiTimeout = config.ApiTimeout;
			ApiToken = config.ApiToken;
			AppId = config.AppId;
			BaseUri = config.BaseUri;
			BlockingScore = config.BlockingScore;
			CaptchaEnabled = config.CaptchaEnabled;
			CookieKey = config.CookieKey;
			CookieName = config.CookieName;
			CssRef = config.CssRef;
			CustomLogo = config.CustomLogo;
			Enabled = config.Enabled;
			EncryptionEnabled = config.EncryptionEnabled;
			FileExtWhitelist = config.FileExtWhitelist;
			JsRef = config.JsRef;
			MonitorMode = config.MonitorMode;
			RemoteConfigurationEnabled = config.RemoteConfigurationEnabled;
			ReporterApiTimeout = config.ReporterApiTimeout;
			RoutesWhitelist = config.RoutesWhitelist;
			SendBlockActivites = config.SendBlockActivites;
			SendPageActivites = config.SendPageActivites;
			SensitiveHeaders = config.SensitiveHeaders;
			SensitiveRoutes = config.SensitiveRoutes;
			SignedWithIP = config.SignedWithIP;
			SignedWithUserAgent = config.SignedWithUserAgent;
			SocketIpHeaders = config.SocketIpHeaders;
			SuppressContentBlock = config.SuppressContentBlock;
			UserAgentOverride = config.UserAgentOverride;
			UseragentsWhitelist = config.UseragentsWhitelist;
		}
	}
}
