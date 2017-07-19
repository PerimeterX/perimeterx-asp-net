using System;
using System.Collections.Specialized;
using System.Diagnostics;

namespace PerimeterX
{
    public class PXConfigurationWrapper
    {
		public bool Enabled { get; set; }
		public string AppId { get; set; }
		public string CookieName { get; set; }
		public string CookieKey { get; set; }
		public bool EncryptionEnabled { get; set; }
		public bool CaptchaEnabled { get; set; }
		public bool SignedWithUserAgent { get; set; }
		public bool SignedWithIP { get; set; }
		public int BlockingScore { get; set; }
		public string ApiToken { get; set; }
		public string BaseUri { get; set; }
		public int ApiTimeout { get; set; }
		public int ReporterApiTimeout { get; set; }
		public StringCollection SocketIpHeader { get; set; }
		public bool SuppressContentBlock { get; set; }
		public int ActivitiesCapacity { get; set; }
		public int ActivitiesBulkSize { get; set; }
		public bool SendPageActivites { get; set; }
		public bool SendBlockActivites { get; set; }
		public StringCollection SensitiveHeaders { get; set; }
		public StringCollection FileExtWhitelist { get; set; }
		public StringCollection RoutesWhitelist { get; set; }
		public StringCollection UseragentsWhitelist { get; set; }
		public string CustomLogo { get; set; }
		public string CssRef { get; set; }
		public string JsRef { get; set; }
		public string UserAgentOverride { get; set; }
		public bool MonitorMode { get; set; }
		public StringCollection SensitiveRoutes { get; set; }
		public bool RemoteConfigurationEnabled { get; set; }
		public string Checksum { get; set; }
		public int RemoteConfigurationInterval { get; set; }
		public int RemoteConfigurationDelay { get; set; }

		public PXConfigurationWrapper(PxModuleConfigurationSection config)
        {
            Enabled = config.Enabled;
			AppId = config.AppId;
			CookieName = config.CookieName;
			CookieKey = config.CookieKey;
			EncryptionEnabled = config.EncryptionEnabled;
			CaptchaEnabled = config.CaptchaEnabled;
			SignedWithUserAgent = config.SignedWithUserAgent;
			SignedWithIP = config.SignedWithIP;
			BlockingScore = config.BlockingScore;
			ApiToken = config.ApiToken;
			BaseUri = config.BaseUri;
			ApiTimeout = config.ApiTimeout;
			ReporterApiTimeout = config.ReporterApiTimeout;
			SocketIpHeader = config.SocketIpHeaders;
			SuppressContentBlock = config.SuppressContentBlock;
			ActivitiesCapacity = config.ActivitiesCapacity;
			ActivitiesBulkSize = config.ActivitiesBulkSize;
			SendPageActivites = config.SendPageActivites;
			SendBlockActivites = config.SendBlockActivites;
			SensitiveHeaders = config.SensitiveHeaders;
			FileExtWhitelist = config.FileExtWhitelist;
			RoutesWhitelist = config.RoutesWhitelist;
			UseragentsWhitelist = config.UseragentsWhitelist;
			CustomLogo = config.CustomLogo;
			CssRef = config.CssRef;
			JsRef = config.JsRef;
			UserAgentOverride = config.UserAgentOverride;
			MonitorMode = config.MonitorMode;
			SensitiveRoutes = config.SensitiveRoutes;
			RemoteConfigurationEnabled = config.RemoteConfigurationEnabled;
            RemoteConfigurationInterval = config.RemoteConfigurationInterval;
            RemoteConfigurationDelay = config.RemoteConfigurationDelay;
        }

        public void Update(PXDynamicConfiguration dynamicConfig)
        {
            Debug.WriteLine("New configurations were found, updating configuration");
			AppId = dynamicConfig.AppId;
			Enabled = dynamicConfig.ModuleEnabled;
			CookieKey = dynamicConfig.CookieKey;
			BlockingScore = dynamicConfig.BlockingScore;
			MonitorMode = dynamicConfig.ModuleMode.Equals("monitoring");
			SocketIpHeader = PxConstants.ArrayToStringCollection(dynamicConfig.IpHeaders);
			SensitiveHeaders = PxConstants.ArrayToStringCollection(dynamicConfig.SensitiveHeaders);
			ApiTimeout = dynamicConfig.RiskTimeout;
			Checksum = dynamicConfig.Checksum;  
        }
    }
}
