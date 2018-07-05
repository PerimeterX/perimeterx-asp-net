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
		void ReversePxCaptcha(HttpContext context);
		bool ShouldReverseClient(HttpContext context);
		bool ShouldReverseXhr(HttpContext context);
		bool ShouldReverseCaptcha(HttpContext context);
	}

	public class ReverseProxy : IReverseProxy
	{
		private readonly string DEFAULT_CLIENT_VALUE = string.Empty;
		private readonly string CONTENT_TYPE_JAVASCRIPT = "application/javascript";
		private readonly string DEFAULT_JSON_VALUE = "{}";
		private readonly string CONTENT_TYPE_JSON = "application/json";
		private readonly byte[] DEFAULT_EMPTY_GIF_VALUE = { 0x47 , 0x49, 0x46, 0x38, 0x39, 0x61, 0x01, 0x00, 0x01, 0x00, 0x80, 0x00,
		0x00, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x2c, 0x00, 0x00, 0x00, 0x00,
		0x01, 0x00, 0x01, 0x00, 0x00, 0x02, 0x02, 0x44, 0x01, 0x00, 0x3b };
		private readonly string CONTENT_TYPE_GIF = "image/gif";

		private readonly string XHR_PATH = "/xhr";
		private readonly string CLIENT_FP_PATH = "/init.js";
		private readonly string CLIENT_TP_PATH = "/main.min.js";
		private readonly string CAPTCHA_FP_PATH = "/captcha";

		private string ClientReversePrefix;
		private string XhrReversePrefix;
		private string CaptchaReversePrefix;
		private string CollectorUrl;

		public bool IsReusable
		{
			get { return true; }
		}

		private PxModuleConfigurationSection PxConfig { get; set; }

		public ReverseProxy(PxModuleConfigurationSection pxConfig)
		{
			PxConfig = pxConfig;
			string appIdPrefix = pxConfig.AppId.Substring(2);
			ClientReversePrefix = "/" + appIdPrefix + CLIENT_FP_PATH;
			XhrReversePrefix = "/" + appIdPrefix + XHR_PATH;
			CaptchaReversePrefix = "/" + appIdPrefix + CAPTCHA_FP_PATH;
			CollectorUrl = string.Format(pxConfig.CollectorUrl, PxConfig.AppId);
		}

		/**
		 *  <summary>
		 *  Reversing an HttpContext.Request and returns a 
		 *  boolean value indicating whether reverse-proxy was 
		 *  successful
		 * </summary>
		 * <param name="context">The original request context</param>
		 * <param name="serverUrl">string value of the remote server's url</param>
		 * <param name="uri">string value for the remote server's uri</param>
		 * <returns>boolean</returns>
		 */
		private bool ProcessRequest(HttpContext context, string serverUrl, string uri)
		{
			try
			{
				// Set headers
				context.Request.Headers.Add(PxConstants.ENFORCER_TRUE_IP_HEADER, PxCommonUtils.GetRequestIP(context, PxConfig));
				context.Request.Headers.Add(PxConstants.FIRST_PARTY_HEADER, PxConstants.FIRST_PARTY_VALUE);

				// Create a connection to the Remote Server to redirect all requests
				RemoteServer server = new RemoteServer(context, serverUrl, uri);

				// Send the request to the remote server and return the response
				HttpWebResponse response = server.GetResponse(server.GetRequest());
				if (response == null || !response.StatusCode.Equals(HttpStatusCode.OK))
				{
					Debug.WriteLine("ReverseProxy responeded with none 200 status", PxConstants.LOG_CATEGORY);
					Debug.WriteLineIf(response != null, "Response status {0}", PxConstants.LOG_CATEGORY);
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
		 * Reverse requests for PerimeterX client
		 * </summary>
		 * <param name="context">The original request context</param>
		 */
		public void ReversePxClient(HttpContext context)
		{
			Debug.WriteLine("Fetching Client", PxConstants.LOG_CATEGORY);
			if (!PxConfig.FirstPartyEnabled)
			{
				Debug.WriteLine("First party is disabled, rendering default JS client response", PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, CONTENT_TYPE_JAVASCRIPT, DEFAULT_CLIENT_VALUE);
				return;
			}
			string uri = "/" + PxConfig.AppId + CLIENT_TP_PATH;
			bool success = ProcessRequest(context, PxConfig.ClientHostUrl, uri);

			if (!success)
			{
				Debug.WriteLine("Redirect JS client returned bad status, rendering default response", PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, CONTENT_TYPE_JAVASCRIPT, DEFAULT_CLIENT_VALUE);
			}

		}

		/**
		 * <summary>
		 * Reverse requests for PerimeterX captcha client
		 * </summary>
		 * <param name="context">The original request context</param>
		 */
		public void ReversePxCaptcha(HttpContext context)
		{
			Debug.WriteLine("Fetching Captcha client", PxConstants.LOG_CATEGORY);
			if (!PxConfig.FirstPartyEnabled)
			{
				Debug.WriteLine("First party is disabled, rendering default captcha client response", PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, CONTENT_TYPE_JAVASCRIPT, DEFAULT_CLIENT_VALUE);
				return;
			}

			string uri = context.Request.RawUrl.Replace(CaptchaReversePrefix, "");


			bool success = ProcessRequest(context, PxConfig.CaptchaHostUrl, uri);

			if (!success)
			{
				Debug.WriteLine("Redirect JS client returned bad status, rendering default response", PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, CONTENT_TYPE_JAVASCRIPT, DEFAULT_CLIENT_VALUE);
			}

		}

		/**
		 * <summary>
		 * Reverse proxy any sensor activities back to PerimeterX servers
		 * </summary>
		 * <param name="context">The original request context</param>
		 */
		public void ReversePxXhr(HttpContext context)
		{
			string defaultResponse = DEFAULT_JSON_VALUE;
			string contentType = CONTENT_TYPE_JSON;
			if (context.Request.Url.AbsolutePath.EndsWith(".gif"))
			{
				defaultResponse = Encoding.Default.GetString(DEFAULT_EMPTY_GIF_VALUE);
				contentType = CONTENT_TYPE_GIF;
			}

			if (!PxConfig.FirstPartyEnabled || !PxConfig.FirstPartyXhrEnabled)
			{
				Debug.WriteLine(string.Format("First party is disabled, rendering default response with Content-Type: {0}", contentType), PxConstants.LOG_CATEGORY);
				RenderPredefinedResponse(context, contentType, defaultResponse);
				return;
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
				Debug.WriteLine(string.Format("Found VID on request, the following VID will be attached to the request: {0}", vid), PxConstants.LOG_CATEGORY);
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
		 * Checks if this is a first party route for the Client JS sensor.
		 * If the route matches the prefix, the module will redirect the request
		 * </summary>
		 * <param name="context">The original request context</param>
		 * <returns>boolean</returns>
		 */
		public bool ShouldReverseClient(HttpContext context)
		{
			if(context.Request.Url.AbsolutePath.Equals(ClientReversePrefix))
			{
				ReversePxClient(context);
				context.ApplicationInstance.CompleteRequest();
				return true;
			}
			return false;
		}

		/**
		 * <summary>
		 * Checks if this is a first party route for the Captcha js file.
		 * If the route matches the prefix, the module will redirect the request
		 * </summary>
		 * <param name="context">The original request context</param>
		 * <returns>boolean</returns>
		 */
		public bool ShouldReverseCaptcha(HttpContext context)
		{
			if (context.Request.Url.AbsolutePath.Equals(CaptchaReversePrefix))
			{
				ReversePxCaptcha(context);
				context.ApplicationInstance.CompleteRequest();
				return true;
			}
			return false;
		}

		/**
		* <summary>
		* Checks if this is a first party route for XHR requests
		* If the route matches the prefix, the module will redirect the request
		* </summary>
		 * <param name="context">The original request context</param>
		* <returns>boolean</returns>
		*/
		public bool ShouldReverseXhr(HttpContext context)
		{
			if(context.Request.Url.AbsolutePath.StartsWith(XhrReversePrefix))
			{
				ReversePxXhr(context);
				context.ApplicationInstance.CompleteRequest();
				return true;
			}
			return false;
		}


		/**
		 * <summary>
		 * Helper function renders a predefined request, this request will also finish the request and return
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
