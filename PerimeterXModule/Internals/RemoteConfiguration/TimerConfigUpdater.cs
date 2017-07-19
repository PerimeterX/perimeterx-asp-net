using System.Timers;

namespace PerimeterX
{
	public class TimerConfigUpdater
	{
		private RemoteConfigurationManager remoteConfigManager;
		private PXConfigurationWrapper pxConfiguration;

		public TimerConfigUpdater(PXConfigurationWrapper pxConfiguration, RemoteConfigurationManager remoteConfigManager)
		{
			this.remoteConfigManager = remoteConfigManager;
			this.pxConfiguration = pxConfiguration;
		}

		public void Schedule()
		{
			Timer timer = new Timer(pxConfiguration.RemoteConfigurationInterval);

			timer.Elapsed += OnTimedEvent;
			timer.Enabled = true;
		}

		private void OnTimedEvent(object source, ElapsedEventArgs e)
		{
			PXDynamicConfiguration dynamicConfig = remoteConfigManager.GetConfiguration();
			if (dynamicConfig != null)
			{
				remoteConfigManager.UpdateConfiguration(dynamicConfig);
			}
			else if (pxConfiguration.Checksum == null)
			{
				remoteConfigManager.DisableModuleOnError();
			}
		}

	}
}
