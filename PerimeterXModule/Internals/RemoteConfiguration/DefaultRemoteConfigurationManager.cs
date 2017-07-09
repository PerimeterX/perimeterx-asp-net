using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jil;

namespace PerimeterX
{
    public class DefaultRemoteConfigurationManager : RemoteConfigurationManager
    {
        private PXConfigurationWrapper pxConfig;
        private HttpClient httpClient;

        public DefaultRemoteConfigurationManager(PXConfigurationWrapper config, HttpClient httpClient)
        {
            Debug.WriteLine("DefaultRemoteConfigurationManager[init]");
            this.pxConfig = config;
            this.httpClient = httpClient;
        }

        public void GetConfigurationFromServer(object state)
        {
            try{
				Debug.WriteLine("DefaultRemoteConfigurationManager[GetConfigurationFromServer]");
				// Prepare url params
				string urlParam = "";
				if (!string.IsNullOrEmpty(pxConfig.Checksum))
				{
					Debug.WriteLine("DefaultRemoteConfigurationManager[GetConfigurationFromServer]: adding checksum");
					urlParam = string.Format("checksum={0}", pxConfig.Checksum);
				}
				string requestUrl = string.Format("{0}{1}{2}", PxConstants.REMOTE_CONFIGURATION_SERVER, PxConstants.REMOTE_CONFIGURATION_PATH, urlParam);

				// Set request
				var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
				var httpResponse = httpClient.SendAsync(requestMessage).Result;

				// Handle response
				if (httpResponse.StatusCode >= HttpStatusCode.NoContent)
				{
					Debug.WriteLineIf(httpResponse.StatusCode.Equals(HttpStatusCode.NoContent), "Got configuration but no updates");
					//Disable module if its first time run
				    DisableModuleOnError();

				}
				else if (httpResponse.StatusCode.Equals(HttpStatusCode.OK))
				{
					var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
                    var pxDynamicConfiguration = JSON.Deserialize<PXDynamicConfiguration>(responseJson, PxConstants.JSON_OPTIONS);
                    pxConfig.Update(pxDynamicConfiguration);
		
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
				Debug.WriteLine("DefaultRemoteConfigurationManager[GetConfigurationFromServer]: disabling module until getting configuration from server");
				pxConfig.Enabled = false;
			}
        }
    }
}
