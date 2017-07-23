using Jil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PerimeterX
{
	class PXHttpClient : IPXHttpClient, IDisposable
	{
		HttpClient httpClient;

		public PXHttpClient(IPXConfiguration config)
		{
			var webRequestHandler = new WebRequestHandler
			{
				AllowPipelining = true,
				UseDefaultCredentials = true,
				UnsafeAuthenticatedConnectionSharing = true
			};

			httpClient = new HttpClient(webRequestHandler, true);
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiToken); ;
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			httpClient.DefaultRequestHeaders.ExpectContinue = false;
		}

		public CaptchaResponse SendCaptchaApi(string url, CaptchaRequest request, int timeout)
		{
			var requestJson = JSON.Serialize(request, PxConstants.JSON_OPTIONS);
			var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
			requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
			CancellationTokenSource source = new CancellationTokenSource(timeout);
			CancellationToken token = source.Token;

			var httpResponse = httpClient.SendAsync(requestMessage, token).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", url, requestJson, responseJson), PxConstants.LOG_CATEGORY);
			return JSON.Deserialize<CaptchaResponse>(responseJson, PxConstants.JSON_OPTIONS);
		}

		public RiskResponse SendRiskApi(string url, RiskRequest riskRequest, int timeout)
		{
			string requestJson = JSON.SerializeDynamic(riskRequest, PxConstants.JSON_OPTIONS);
			var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
			};
			CancellationTokenSource source = new CancellationTokenSource(timeout);
			CancellationToken token = source.Token;

			var httpResponse = httpClient.SendAsync(requestMessage, token).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", PxConstants.RISK_API_V2, requestJson, responseJson), PxConstants.LOG_CATEGORY);
			return JSON.Deserialize<RiskResponse>(responseJson, PxConstants.JSON_OPTIONS);
		}

		public RemoteConfigurationResponse GetConfiguration(string url, string path, string checksum)
		{
			var uriBuilder = new UriBuilder(url);
			uriBuilder.Path = path;
	
			if (!string.IsNullOrEmpty(checksum))
			{
				var query = HttpUtility.ParseQueryString(uriBuilder.Query);
				query["checksum"] = checksum;
				uriBuilder.Query = query.ToString();
			}
			url = uriBuilder.ToString();
			Debug.WriteLine(string.Format("GET request for {0}", url), PxConstants.LOG_CATEGORY);
			HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

			var httpResponse = httpClient.SendAsync(requestMessage).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("GET request for {0}, returned status code {1} and json {2}", url, httpResponse.StatusCode, responseJson), PxConstants.LOG_CATEGORY);
			RemoteConfigurationResponse remoteConfiguration = null;
			if (httpResponse.StatusCode.Equals(HttpStatusCode.OK))
			{
				remoteConfiguration = JSON.Deserialize<RemoteConfigurationResponse>(responseJson, PxConstants.JSON_OPTIONS);
			}
			return remoteConfiguration;
		}

		public void Dispose()
		{
			httpClient.Dispose();
		}
	}
}
