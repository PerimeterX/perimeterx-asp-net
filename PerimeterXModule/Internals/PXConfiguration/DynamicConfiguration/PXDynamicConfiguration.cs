using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Timers;

namespace PerimeterX
{
	public class PXDynamicConfiguration : IPXConfiguration
	{
		public int ActivitiesBulkSize { get; private set; }
		public int ActivitiesCapacity { get; private set; }
		public int ApiTimeout { get; private set; }
		public string ApiToken { get; private set; }
		public string AppId { get; private set; }
		public string BaseUri { get; private set; }
		public int BlockingScore { get; private set; }
		public bool CaptchaEnabled { get; private set; }
		public string CookieKey { get; private set; }
		public string CookieName { get; private set; }
		public string CssRef { get; private set; }
		public string CustomLogo { get; private set; }
		public bool Enabled { get; private set; }
		public bool EncryptionEnabled { get; private set; }
		public StringCollection FileExtWhitelist { get; private set; }
		public string JsRef { get; private set; }
		public bool MonitorMode { get; private set; }
		public bool RemoteConfigurationEnabled { get; private set; }
		public int ReporterApiTimeout { get; private set; }
		public StringCollection RoutesWhitelist { get; private set; }
		public bool SendBlockActivites { get; private set; }
		public bool SendPageActivites { get; private set; }
		public StringCollection SensitiveHeaders { get; private set; }
		public StringCollection SensitiveRoutes { get; private set; }
		public bool SignedWithIP { get; private set; }
		public bool SignedWithUserAgent { get; private set; }
		public StringCollection SocketIpHeaders { get; private set; }
		public bool SuppressContentBlock { get; private set; }
		public string UserAgentOverride { get; private set; }
		public StringCollection UseragentsWhitelist { get; private set; }
		public string RemoteConfigurationUrl { get; private set; }
		public string RemoteConfigurationPath { get; private set; }
		public int RemoteConfigurationInterval { get; set; }

		private string checksum;
		private IPXHttpClient pxHttpClient;

		public PXDynamicConfiguration(PxModuleConfigurationSection config)
		{
			Debug.WriteLine("Dynamic configuration loaded", PxConstants.LOG_CATEGORY);
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
			RemoteConfigurationUrl = config.RemoteConfigurationUrl;
			RemoteConfigurationPath = config.RemoteConfigurationPath;
			RemoteConfigurationInterval = config.RemoteConfigurationInterval;

			pxHttpClient = new PXHttpClient(this);

			Timer timer = new Timer()
			{
				Enabled = true,
				AutoReset = true,
				Interval = RemoteConfigurationInterval
			};
			timer.Elapsed += (obj, e) => UpdateConfigurationTask();
			
		}
		
		private void UpdateConfigurationTask()
		{
			try
			{
				RemoteConfigurationResponse remoteConfiguration = pxHttpClient.GetConfiguration(RemoteConfigurationUrl, RemoteConfigurationPath, checksum);
				if (remoteConfiguration != null)
				{
					Debug.WriteLine("New configuration found, updating current configuration");
					Update(remoteConfiguration);
				} 
				else if (string.IsNullOrEmpty(checksum))
				{
					DisableModuleOnError();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(string.Format("An exception was caught during configuraiton fetch, {0}", ex.Message), PxConstants.LOG_CATEGORY);
				if (string.IsNullOrEmpty(checksum))
				{
					DisableModuleOnError();
				}
			}
		}

		private void Update(RemoteConfigurationResponse remoteConfiguration)
		{
			Enabled = remoteConfiguration.ModuleEnabled;
			CookieKey = remoteConfiguration.CookieKey;
			BlockingScore = remoteConfiguration.BlockingScore;
			AppId = remoteConfiguration.AppId;
			MonitorMode = remoteConfiguration.ModuleMode == "monitoring";
			ApiTimeout = remoteConfiguration.RiskTimeout;
			checksum = remoteConfiguration.Checksum;

			SocketIpHeaders.Clear();
			SocketIpHeaders.AddRange(remoteConfiguration.IpHeaders);

			SensitiveHeaders.Clear();
			SensitiveHeaders.AddRange(remoteConfiguration.SensitiveHeaders);
		}

		private void DisableModuleOnError()
		{
			Debug.WriteLine("Disabling PxModule because failed to get configruation", PxConstants.LOG_CATEGORY);
			Enabled = false;
		}
	}
}
