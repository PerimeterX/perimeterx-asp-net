using System;
using System.Threading;

namespace PerimeterX
{
    public class TimerConfigUpdater
    {
		private RemoteConfigurationManager remoteConfigManager;
        private Timer timer;
            
		public TimerConfigUpdater(RemoteConfigurationManager remoteConfigManager)
        {
           this.remoteConfigManager = remoteConfigManager;
        }

        public void Schedule()
        {
            timer = new Timer(remoteConfigManager.GetConfigurationFromServer, null, 0, 5000);
        }
    }
}
