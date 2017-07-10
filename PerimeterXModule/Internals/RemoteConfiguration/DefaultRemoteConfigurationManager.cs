using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jil;

namespace PerimeterX
{
    public class DefaultRemoteConfigurationManager : RemoteConfigurationManager
    {

        private PXConfigurationWrapper pxConfig;
        public PxClient pxClient;

        public DefaultRemoteConfigurationManager(PXConfigurationWrapper pxConfig, PxClient pxClient)
        {
            Debug.WriteLine("DefaultRemoteConfigurationManager[init]");
            this.pxConfig = pxConfig;
            this.pxClient = pxClient;
        }

        public void GetConfigurationFromServer(object state)
        {
            try{
                Debug.WriteLine(string.Format("DefaultRemoteConfigurationManager[GetConfigurationFromServer]"));
				// Prepare url params
				string checksumParam = "";
				if (!string.IsNullOrEmpty(pxConfig.Checksum))
				{
					Debug.WriteLine("DefaultRemoteConfigurationManager[GetConfigurationFromServer]: adding checksum");
					checksumParam = string.Format("?checksum={0}", pxConfig.Checksum);
				}
                var dynamicConfig = pxClient.GetConfigurationRequest(checksumParam);

                if (dynamicConfig != null){
                    pxConfig.Update(dynamicConfig);
                }

            }

            catch (AggregateException ex)
            {
                if ((ex.InnerException is TaskCanceledException) && (string.IsNullOrEmpty(pxConfig.Checksum)))
                {
                    DisableModuleOnError();       
                }
			}
			
        }


        private void DisableModuleOnError(){
			if (string.IsNullOrEmpty(pxConfig.Checksum) && !pxConfig.Enabled)
			{
				Debug.WriteLine("DefaultRemoteConfigurationManager[GetConfigurationFromServer]: checksum is empty, disabling module until getting configuration from server");
				pxConfig.Enabled = false;
			}
        }

    }
}
