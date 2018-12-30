using System;
using System.Diagnostics;
using System.Net.Http;

namespace PerimeterX
{
    public class HttpHandler
    {
        private HttpClient httpClient;
        private string baseUri;

        public HttpHandler(PxModuleConfigurationSection config, string baseUri, int timeout)
        {
            this.httpClient = PxConstants.CreateHttpClient(false, timeout, true, config);
            this.baseUri = baseUri;
        }

        public string Post(string requestJson, string uri)
        {
            try
            {
                // Create POST request
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, baseUri + uri);
                requestMessage.Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

                // Send the request
                var httpResponse = httpClient.SendAsync(requestMessage).Result;
                httpResponse.EnsureSuccessStatusCode();
                var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
                PxLoggingUtils.LogDebug(string.Format("Post request for {0} ({1}), returned {2}", uri, requestJson, responseJson));

                return responseJson;
            }
            catch (Exception ex)
            {
                PxLoggingUtils.LogDebug(string.Format("Failed sending POST request for {0} ({1}), returned error: {2}", uri, requestJson, ex.Message));
                throw ex;
            }
        }

		public void Dispose()
        {
            this.httpClient.Dispose();
            this.httpClient = null;
        }
    }
}
