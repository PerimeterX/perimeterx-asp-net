using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PerimeterX
{
	internal class RemoteServer
	{
		private readonly int BUFFER_SIZE = 256;
		private readonly string[] RESTRICTED_HEADERS = new string[] {
			"Connection", "Content-Length", "Date", "Expect", "Host",
			"If-Modified-Since", "Range", "Transfer-Encoding", "Proxy-Connection",
			"Accept", "Content-Type", "Referer", "User-Agent"
		};

		private string remoteUrl;
		private HttpContext context;

		/// <summary>
		/// Initialize the communication with the Remote Server
		/// </summary>
		/// <param name="context">Context</param>
		public RemoteServer(HttpContext context, string serverUrl, string uri)
		{
			this.context = context;
			remoteUrl = serverUrl + uri;
		}

		/// <summary>
		/// Create a request the remote server
		/// </summary>
		/// <returns>Request to send to the server </returns>
		public HttpWebRequest GetRequest()
		{
			CookieContainer cookieContainer = new CookieContainer();

			// Create a request to the server
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(remoteUrl);

			// Set options
			var requestHeaders = context.Request.Headers;
			var proxyHeaders = new WebHeaderCollection();

			foreach (string headerName in requestHeaders)
			{
				// Skip assigment for a restriected header
				// Let IIS handle it or set it manually
				if (RESTRICTED_HEADERS.Contains(headerName))
				{
					continue;
				}

				proxyHeaders.Add(headerName, requestHeaders.Get(headerName));
			}

			request.Headers = proxyHeaders;
			request.KeepAlive = true;

			request.Method = context.Request.HttpMethod;
			request.UserAgent = context.Request.UserAgent;
			request.Referer = context.Request.Headers.Get("Referer");
			request.ContentType = context.Request.ContentType;
			request.Accept = context.Request.Headers.Get("Accept");

			// For POST, write the post data extracted from the incoming request
			if (request.Method == "POST")
			{
				Stream clientStream = context.Request.InputStream;
				byte[] clientPostData = new byte[context.Request.InputStream.Length];
				clientStream.Read(clientPostData, 0, (int)context.Request.InputStream.Length);

				request.ContentType = context.Request.ContentType;
				request.ContentLength = clientPostData.Length;
				Stream stream = request.GetRequestStream();
				stream.Write(clientPostData, 0, clientPostData.Length);
				stream.Close();
			}

			return request;

		}

		/// <summary>
		/// Send the request to the remote server and return the response
		/// </summary>
		/// <param name="request">Request to send to the server </param>
		/// <returns>Response received from the remote server or null on error </returns>
		public HttpWebResponse GetResponse(HttpWebRequest request)
		{
			HttpWebResponse response;

			try
			{
				response = (HttpWebResponse)request.GetResponse();
			}
			catch (WebException e)
			{
				PxLoggingUtils.LogError("Failed to get response: " + e.Message);
				return null;
			}

			return response;
		}

		/// <summary>
		/// Return the response in bytes array format
		/// </summary>
		/// <param name="response">Response received from the remote server </param>
		/// <returns>Response in bytes </returns>
		public byte[] GetResponseStreamBytes(HttpWebResponse response)
		{
			byte[] buffer = new byte[BUFFER_SIZE];
			MemoryStream memoryStream = new MemoryStream();

			Stream responseStream = response.GetResponseStream();
			int remoteResponseCount = responseStream.Read(buffer, 0, BUFFER_SIZE);

			while (remoteResponseCount > 0)
			{
				memoryStream.Write(buffer, 0, remoteResponseCount);
				remoteResponseCount = responseStream.Read(buffer, 0, BUFFER_SIZE);
			}

			byte[] responseData = memoryStream.ToArray();

			memoryStream.Close();
			responseStream.Close();

			memoryStream.Dispose();
			responseStream.Dispose();

			return responseData;
		}

		/// <summary>
		/// Set cookies received from remote server to response of navigator
		/// </summary>
		/// <param name="response">Response received from the remote server</param>
		public void SetContextCookies(HttpWebResponse response)
		{
			context.Response.Cookies.Clear();
			foreach (Cookie receivedCookie in response.Cookies)
			{
				HttpCookie c = new HttpCookie(receivedCookie.Name, receivedCookie.Value);
				c.Domain = context.Request.Url.Host;
				c.Expires = receivedCookie.Expires;
				c.HttpOnly = receivedCookie.HttpOnly;
				c.Path = receivedCookie.Path;
				c.Secure = receivedCookie.Secure;
				context.Response.Cookies.Add(c);
			}
		}
	}
}
