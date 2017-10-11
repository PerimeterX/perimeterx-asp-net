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
	class PXHttpClient : IPXHttpClient
	{
		HttpClient httpClient;

		public PXHttpClient(string apiToken)
		{
			var webRequestHandler = new WebRequestHandler
			{
				AllowPipelining = true,
				UseDefaultCredentials = true,
				UnsafeAuthenticatedConnectionSharing = true
			};

			httpClient = new HttpClient(webRequestHandler, true);
			httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken); ;
			httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			httpClient.DefaultRequestHeaders.ExpectContinue = false;
		}

		public CaptchaResponse SendCaptchaApi(string url, string path, CaptchaRequest request, int timeout)
		{
			var uriBuilder = new UriBuilder(url)
			{
				Path = path
			};
			string uri = uriBuilder.ToString();
			var requestJson = JSON.Serialize(request, PxConstants.JSON_OPTIONS);
			var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
			requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
			CancellationTokenSource source = new CancellationTokenSource(timeout);
			CancellationToken token = source.Token;

			var httpResponse = httpClient.SendAsync(requestMessage, token).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", uri, requestJson, responseJson), PxConstants.LOG_CATEGORY);
			return JSON.Deserialize<CaptchaResponse>(responseJson, PxConstants.JSON_OPTIONS);
		}

		public RiskResponse SendRiskApi(string url, string path, RiskRequest riskRequest, int timeout)
		{
			var uriBuilder = new UriBuilder(url)
			{
				Path = path
			};
			string uri = uriBuilder.ToString();

			string requestJson = JSON.SerializeDynamic(riskRequest, PxConstants.JSON_OPTIONS);
			var requestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
			{
				Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
			};
			CancellationTokenSource source = new CancellationTokenSource(timeout);
			CancellationToken token = source.Token;

			var httpResponse = httpClient.SendAsync(requestMessage, token).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", uri, requestJson, responseJson), PxConstants.LOG_CATEGORY);
			return JSON.Deserialize<RiskResponse>(responseJson, PxConstants.JSON_OPTIONS);
		}

		public RemoteConfigurationResponse GetConfiguration(string url, string path, string checksum)
		{
			var uriBuilder = new UriBuilder(url)
			{
				Path = path
			};

			if (!string.IsNullOrEmpty(checksum))
			{
				var query = HttpUtility.ParseQueryString(uriBuilder.Query);
				query["checksum"] = checksum;
				uriBuilder.Query = query.ToString();
			}
			string uri = uriBuilder.ToString();
			Debug.WriteLine(string.Format("GET request for {0}", uri), PxConstants.LOG_CATEGORY);
			HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

			var httpResponse = httpClient.SendAsync(requestMessage).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("GET request for {0}, returned status code {1} and json {2}", uri, httpResponse.StatusCode, responseJson), PxConstants.LOG_CATEGORY);
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
