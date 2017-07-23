using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Timers;

namespace PerimeterX
{
	public class PXDynamicConfiguration : IPXConfiguration, IDisposable
	{
		private static ReaderWriterLock rwl = new ReaderWriterLock();

		public int ActivitiesBulkSize { get { return configSection.ActivitiesBulkSize; } }
		public int ActivitiesCapacity { get { return configSection.ActivitiesCapacity; } }
		public string ApiToken { get { return configSection.ApiToken; } }
		public string BaseUri { get { return configSection.BaseUri; } }
		public bool CaptchaEnabled { get { return configSection.CaptchaEnabled; } }
		public string CookieName { get { return configSection.CookieName; } }
		public string CssRef { get { return configSection.CssRef; } }
		public string CustomLogo { get { return configSection.CustomLogo; } }
		public bool EncryptionEnabled { get { return configSection.EncryptionEnabled; } }
		public StringCollection FileExtWhitelist { get { return configSection.FileExtWhitelist; } }
		public string JsRef { get { return configSection.JsRef; } }
		public bool RemoteConfigurationEnabled { get { return configSection.RemoteConfigurationEnabled; } }
		public int ReporterApiTimeout { get { return configSection.ReporterApiTimeout; } }
		public StringCollection RoutesWhitelist { get { return configSection.RoutesWhitelist; } }
		public bool SendBlockActivites { get { return configSection.SendBlockActivites; } }
		public bool SendPageActivites { get { return configSection.SendPageActivites; } }
		public StringCollection SensitiveRoutes { get { return configSection.SensitiveRoutes; } }
		public bool SignedWithIP { get { return configSection.SignedWithIP; } }
		public bool SignedWithUserAgent { get { return configSection.SignedWithUserAgent; } }
		public Boolean SuppressContentBlock { get { return configSection.SuppressContentBlock; } }
		public string UserAgentOverride { get { return configSection.UserAgentOverride; } }
		public StringCollection UseragentsWhitelist { get { return configSection.UseragentsWhitelist; } }
		public string RemoteConfigurationUrl { get { return configSection.RemoteConfigurationUrl; } }
		public int RemoteConfigurationInterval { get { return configSection.RemoteConfigurationInterval; } }

		//Dynamic properties 
		public int ApiTimeout { get { return dynamicProperties.ApiTimeout; } }
		public StringCollection SocketIpHeaders { get { return dynamicProperties.SocketIpHeaders; } }
		public StringCollection SensitiveHeaders { get { return dynamicProperties.SensitiveHeaders; } }
		public bool MonitorMode { get { return dynamicProperties.MonitorMode; } }
		public bool Enabled { get { return dynamicProperties.Enabled; } }
		public string CookieKey { get { return dynamicProperties.CookieKey; } }
		public int BlockingScore { get { return dynamicProperties.BlockingScore; } }
		public string AppId { get { return dynamicProperties.AppId; } }

		private IPXHttpClient pxHttpClient;
		private System.Timers.Timer timer;
		
		private PxModuleConfigurationSection configSection;
		private DynamicProperties _dynamicProperties;

		private DynamicProperties dynamicProperties
		{
			get
			{
				try
				{
					rwl.AcquireReaderLock(0);	
					return this._dynamicProperties;
				}
				finally
				{
					rwl.ReleaseReaderLock();
				}
			}
			set
			{
				try
				{
					rwl.AcquireWriterLock(0);
					_dynamicProperties = value;
				}
				finally
				{
					rwl.ReleaseWriterLock();
				}
			}
		}

		public PXDynamicConfiguration(PxModuleConfigurationSection configSection)
		{
			Debug.WriteLine("Loaded Dynamic configuration ", PxConstants.LOG_CATEGORY);
			this.configSection = configSection;

			dynamicProperties = new DynamicProperties()
			{
				ApiTimeout = configSection.ApiTimeout,
				AppId = configSection.AppId,
				BlockingScore = configSection.BlockingScore,
				CookieKey = configSection.CookieKey,
				Enabled = configSection.Enabled,
				MonitorMode = configSection.MonitorMode,
				SensitiveHeaders = configSection.SensitiveHeaders,
				SocketIpHeaders = configSection.SocketIpHeaders
			};

			pxHttpClient = new PXHttpClient(ApiToken);

			timer = new System.Timers.Timer()
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
				RemoteConfigurationResponse remoteConfiguration = pxHttpClient.GetConfiguration(RemoteConfigurationUrl, PxConstants.REMOTE_CONFIG_V1, dynamicProperties.Checksum);
				if (remoteConfiguration != null)
				{
					Debug.WriteLine("New configuration found, updating current configuration");
					Update(remoteConfiguration);
				}
				else if (string.IsNullOrEmpty(dynamicProperties.Checksum))
				{
					DisableModuleOnError();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(string.Format("An exception was caught during configuraiton fetch, {0}", ex.Message), PxConstants.LOG_CATEGORY);
				if (string.IsNullOrEmpty(dynamicProperties.Checksum))
				{
					DisableModuleOnError();
				}
			}
		}

		private void Update(RemoteConfigurationResponse remoteConfiguration)
		{
			DynamicProperties newDynamicProperties = new DynamicProperties()
			{
				ApiTimeout = remoteConfiguration.RiskTimeout,
				AppId = remoteConfiguration.AppId,
				BlockingScore = remoteConfiguration.BlockingScore,
				CookieKey = remoteConfiguration.CookieKey,
				Enabled = remoteConfiguration.ModuleEnabled,
				MonitorMode = remoteConfiguration.ModuleMode == "monitoring",
				SensitiveHeaders = new StringCollection(),
				SocketIpHeaders = new StringCollection(),
				Checksum = remoteConfiguration.Checksum
			};
			newDynamicProperties.SensitiveHeaders.AddRange(remoteConfiguration.SensitiveHeaders);
			newDynamicProperties.SocketIpHeaders.AddRange(remoteConfiguration.IpHeaders);

			dynamicProperties = newDynamicProperties;
		}

		private void DisableModuleOnError()
		{
			Debug.WriteLine("Disabling PxModule because failed to get configruation", PxConstants.LOG_CATEGORY);
			dynamicProperties.Enabled = false;
		}

		public void Dispose()
		{
			timer.Dispose();
		}

		class DynamicProperties
		{
			public int ApiTimeout { get; set; }
			public string AppId { get; set; }
			public int BlockingScore { get; set; }
			public string CookieKey { get; set; }
			public bool Enabled { get; set; }
			public bool MonitorMode { get; set; }
			public StringCollection SensitiveHeaders { get; set; }
			public StringCollection SocketIpHeaders { get; set; }
			public string Checksum { get; set; }
		}
	}
}
