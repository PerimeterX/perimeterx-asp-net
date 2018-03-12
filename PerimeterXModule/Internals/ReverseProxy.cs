using PerimeterX;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;

namespace PerimeterX
{
	public interface IReverseProxy
	{
		void ReversePxClient(HttpContext context);
		void ReversePxXhr(HttpContext context);
		bool ShouldReverseClient(string uri);
		bool ShouldReverseXhr(string uri);
	}

	public class ReverseProxy : IReverseProxy
	{
		private readonly string DEFAULT_CLIENT_VALUE = "";
		private readonly string CONTENT_TYPE_JAVASCRIPT = "application/javascript";
		private readonly string DEFAULT_JSON_VALUE = "{}";
		private readonly string CONTENT_TYPE_JSON = "application/json";
		private readonly byte[] DEFAULT_EMPTY_GIF_VALUE = { 0x47 , 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00,
		0x00, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x2c, 0x00, 0x00, 0x00, 0x00,
		0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3b };
		private readonly string CONTENT_TYPE_GIF = "image/gif";

		private readonly string XHR_PATH = "xhr";
		private readonly string CLIENT_PATH = "init.js";

		private string ClientReversePrefix;
		private string XhrReversePrefix;
		private string CollectorUrl;

		public bool IsReusable
		{
			get { return true; }
		}

		private PxModuleConfigurationSection PxConfig { get; set; }

		public ReverseProxy(PxModuleConfigurationSection pxConfig)
		{
			PxConfig = pxConfig;
			string prefixFormat = "/{0}/{1}";
			string appIdPrefix = pxConfig.AppId.Substring(2);
			ClientReversePrefix = string.Format(prefixFormat, appIdPrefix, CLIENT_PATH);
			XhrReversePrefix = string.Format(prefixFormat, appIdPrefix, XHR_PATH);
			CollectorUrl = string.Format(pxConfig.CollectorUrl, pxConfig.AppId);
		}

		/**
		 *  <summary>
		 * Method calls when client request the server
		 * </summary>
		 * <param name="context">HttpContext</param>
		 * <param name="serverUrl">string</param>
		 * <param name="uri">string</param>
		 * <returns>bool, true if re</returns>
		 */
		private bool ProcessRequest(HttpContext context, string serverUrl, string uri)
		{
			try
			{
				// Set headers
				context.Request.Headers.Add(PxConstants.ENFORCER_TRUE_IP_HEADER, PxCommonUtils.GetRequestIP(context, PxConfig));
				context.Request.Headers.Add(PxConstants.FIRST_PARTY_HEADER, PxConstants.FIRST_PARTY_VALUE);

				// Create a connexion to the Remote Server to redirect all requests
				RemoteServer server = new RemoteServer(context, serverUrl, uri);

				// Create a request with same data in navigator request
				HttpWebRequest request = server.GetRequest();

				// Send the request to the remote server and return the response
				HttpWebResponse response = server.GetResponse(request);
				if (response == null || !response.StatusCode.Equals(HttpStatusCode.OK))
				{
					Debug.WriteLine("ReverseProxy responeded with none 200 status", PxConstants.LOG_CATEGORY);
					Debug.WriteLineIf(!(response == null), "Response status {0}", PxConstants.LOG_CATEGORY);
					return false;
				}

				byte[] responseData = server.GetResponseStreamBytes(response);

				// Send the response to client
				context.Response.Headers.Add(response.Headers);
				context.Response.ContentEncoding = Encoding.UTF8;
				context.Response.ContentType = response.ContentType;
				context.Response.OutputStream.Write(responseData, 0, responseData.Length);

				// Handle cookies to navigator
				server.SetContextCookies(response);

				// Close streams
				response.Close();
				return true;
			}
			catch (Exception e)
			{
				Debug.WriteLine("Unexpected error while processing reverse request: " + e.Message, PxConstants.LOG_CATEGORY);
				return false;
			}

		}
		/**
		 * <summary>
		 * Forward requests for PerimeterX client
		 * </summary>
		 * <param name="context">HttpContext</param>
		 */
		public void ReversePxClient(HttpContext context)
		{
			Debug.WriteLine("Fetching Client", PxConstants.LOG_CATEGORY);
			string contentType = CONTENT_TYPE_JAVASCRIPT;
			string defaultResponse = DEFAULT_CLIENT_VALUE;
			if (!PxConfig.FirstPartyEnabled)
			{
				Debug.WriteLine("First party is disabled, rendering default response", PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, contentType, defaultResponse);
			}
			string uri = string.Format("/{0}/main.min.js", PxConfig.AppId);
			bool success = ProcessRequest(context, PxConfig.ClientHostUrl, uri);

			if (!success)
			{
				Debug.WriteLine("Redirect XHR returned bad status, rendering default response", PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, contentType, defaultResponse);
			}

		}

		/**
		 * <summary>
		 * Forward any sensor activities back to PerimeterX servers
		 * </summary>
		 * <param name="context">HttpContext</param>
		 */
		public void ReversePxXhr(HttpContext context)
		{
			string defaultResponse;
			string contentType;

			if (context.Request.Url.AbsolutePath.EndsWith(".gif"))
			{
				defaultResponse = Encoding.Default.GetString(DEFAULT_EMPTY_GIF_VALUE);
				contentType = CONTENT_TYPE_GIF;
			}
			else
			{
				defaultResponse = DEFAULT_JSON_VALUE;
				contentType = CONTENT_TYPE_JSON;
			}

			if (!PxConfig.FirstPartyEnabled || !PxConfig.FirstPartyXhrEnabled)
			{
				Debug.WriteLine("First party is disabled, rendering default response", PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, contentType, defaultResponse);
			}
			string uri = context.Request.RawUrl.Replace(XhrReversePrefix, "");

			string vid = null;
			HttpCookie pxvid = context.Request.Cookies.Get("pxvid");
			if (pxvid != null)
			{
				vid = pxvid.Value;
			}
			else if ((pxvid = context.Request.Cookies.Get("_pxvid")) != null)
			{
				vid = pxvid.Value;
			}

			// Clear sensitive headers
			foreach (string sensitiveHeader in PxConfig.SensitiveHeaders)
			{
				if (context.Request.Headers[sensitiveHeader] != null)
				{
					context.Request.Headers.Remove(sensitiveHeader);
				}
			}

			if (!string.IsNullOrEmpty(vid))
			{
				Debug.WriteLine(string.Format("Found VID on request, the follwoing VID will be attached to the request: {0}", vid), PxConstants.LOG_CATEGORY);
				context.Request.Headers.Add("Cookie", string.Format("pxvid={0}", vid));
			}

			bool success = ProcessRequest(context, CollectorUrl, uri);
			if (!success)
			{
				Debug.WriteLine("Redirect XHR returned bad status, rendering default response", PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, contentType, defaultResponse);
			}
		}

		/**
		 * <summary>
		 * Checks if this reuqest should be forwarded to PerimeterX
		 * </summary>
		 * <param name="uri">string, absolut uri</param>
		 */
		public bool ShouldReverseClient(string uri)
		{
			return uri.Equals(ClientReversePrefix);
		}

		/**
		* <summary>
		* Checks if this reuqest should be forwarded to PerimeterX
		* </summary>
		* <param name="uri">string, absolut uri</param>
		*/
		public bool ShouldReverseXhr(string uri)
		{
			return uri.StartsWith(XhrReversePrefix);
		}


		/**
		 * <summary>
		 * Helper function redners a predefined request, this request will also finish the request and return
		 * a response
		 * </summary>
		 * <param name="contentType">string, value for Content-Type header</param>
		 * <param name="response">string, value to render on the response</param>
		 */
		private void RenderPredefinedResponse(HttpContext context, string contentType, string response)
		{
			// Send the response to client
			context.Response.ContentEncoding = Encoding.UTF8;
			context.Response.ContentType = contentType;
			context.Response.Write(response);

			context.Response.End();

		}
	}
}
