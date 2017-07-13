using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Jil;
using System.Threading;

namespace PerimeterX
{
    public class DefaultPxClient : PxClient
    {
        private HttpClient httpClient;
        private PXConfigurationWrapper pxConfig;

        public DefaultPxClient(PXConfigurationWrapper pxConfig)
        {
            this.pxConfig = pxConfig;
			var webRequestHandler = new WebRequestHandler
			{
				AllowPipelining = true,
				UseDefaultCredentials = true,
				UnsafeAuthenticatedConnectionSharing = true
			};
			httpClient = new HttpClient(webRequestHandler, true);

			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pxConfig.ApiToken);
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			httpClient.DefaultRequestHeaders.ExpectContinue = false;
        }

        public PXDynamicConfiguration GetConfigurationRequest(string checksumParams)
        {
			string requestUrl = string.Format("{0}{1}{2}", PxConstants.REMOTE_CONFIGURATION_SERVER, PxConstants.REMOTE_CONFIGURATION_PATH, checksumParams);
			Debug.WriteLine(string.Format("Get request for {0}", requestUrl), PxConstants.LOG_CATEGORY);

			// Set request
			var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMilliseconds(pxConfig.ApiTimeout));
			CancellationToken cancellationToken = source.Token;
			var httpResponse = httpClient.SendAsync(requestMessage, cancellationToken).Result;

            Debug.WriteLine(string.Format("response for {0} received status code {1}", requestUrl, httpResponse.StatusCode));

			PXDynamicConfiguration pxDynamicConfiguration = null;
            if (!httpResponse.StatusCode.Equals(HttpStatusCode.OK))
			{
                Debug.WriteLineIf(httpResponse.StatusCode.Equals(HttpStatusCode.NoContent) , "Got configuration but no updates");
				Debug.WriteLineIf(httpResponse.StatusCode > HttpStatusCode.NoContent, "Failed to get configuration");
			}
            else if (httpResponse.StatusCode.Equals(HttpStatusCode.OK))
			{
                var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
                Debug.WriteLine(string.Format("Post request for {0} ), returned {1}", PxConstants.REMOTE_CONFIGURATION_SERVER, responseJson), PxConstants.LOG_CATEGORY);
				return JSON.Deserialize<PXDynamicConfiguration>(responseJson, PxConstants.JSON_OPTIONS);
			}
            return pxDynamicConfiguration;
        }

        public CaptchaResponse SendCaptchaRequest(CaptchaRequest captchaRequest)
        {
            var requestJson = JSON.Serialize(captchaRequest, PxConstants.JSON_OPTIONS);
			var requestMessage = new HttpRequestMessage(HttpMethod.Post, PxConstants.FormatBaseUri(pxConfig) + PxConstants.CAPTCHA_API_V1)
			{
				Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
            };
			CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMilliseconds(pxConfig.ApiTimeout));
			CancellationToken cancellationToken = source.Token;
			var httpResponse = httpClient.SendAsync(requestMessage, cancellationToken).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", PxConstants.JSON_OPTIONS, requestJson, responseJson), PxConstants.LOG_CATEGORY);
			return JSON.Deserialize<CaptchaResponse>(responseJson, PxConstants.JSON_OPTIONS);
        }

        public RiskResponse SendRiskRequest(RiskRequest riskRequest)
        {
            string requestJson = JSON.SerializeDynamic(riskRequest, PxConstants.JSON_OPTIONS);
			var requestMessage = new HttpRequestMessage(HttpMethod.Post, PxConstants.FormatBaseUri(pxConfig) + PxConstants.RISK_API_V2)
			{
				Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
			};
			CancellationTokenSource source = new CancellationTokenSource(TimeSpan.FromMilliseconds(pxConfig.ApiTimeout));
			CancellationToken cancellationToken = source.Token;
			var httpResponse = httpClient.SendAsync(requestMessage, cancellationToken).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", PxConstants.RISK_API_V2, requestJson, responseJson), PxConstants.LOG_CATEGORY);
			return JSON.Deserialize<RiskResponse>(responseJson, PxConstants.JSON_OPTIONS);
        }

    }
}
