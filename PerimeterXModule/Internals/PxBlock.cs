using Jil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX.Internals
{
	class PxBlock
	{
		private HttpClient httpClient;

		public PxBlock(PxModuleConfigurationSection config)
		{
			if (config.CustomBlockUrl != null)
			{
				this.httpClient = PxConstants.CreateHttpClient(false, config.ApiTimeout, false, config);
			}
		}

		public bool IsJsonResponse(PxContext pxContext)
		{
			Dictionary<string, string> headers = pxContext.GetHeadersAsDictionary();
			string jsonHeader;
			bool jsonHeaderExists = headers.TryGetValue("accept", out jsonHeader) || headers.TryGetValue("content-type", out jsonHeader);
			if (jsonHeaderExists)
			{
				string[] values = jsonHeader.Split(',');
				if (Array.Exists(values, element => element == "application/json"))
				{
					return true;
				}
			}

			return false;
		}

		public string injectCaptchaScript(string vid, string uuid)
		{
			return "<script type=\"text/javascript\">window._pxVid = \"" + vid + "\"; window._pxUuid = \"" + uuid + "\";</script>";
		}

		public void ResponseBlockPage(PxContext pxContext, PxModuleConfigurationSection config)
		{
			string template = "block_template";

			if (pxContext.BlockAction == "r")
			{
				template = "ratelimit";
			}

			// In the case of a challenge, the challenge response is taken directly from BlockData. Otherwise, generate html template.
			string content = pxContext.BlockAction == "j" && !string.IsNullOrEmpty(pxContext.BlockData) ? pxContext.BlockData :
				TemplateFactory.getTemplate(template, config, pxContext.UUID, pxContext.Vid, pxContext.IsMobileRequest, pxContext.BlockAction);

			pxContext.ApplicationContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;

			if (pxContext.IsMobileRequest)
			{
				pxContext.ApplicationContext.Response.ContentType = "application/json";
				using (var output = new StringWriter())
				{
					JSON.Serialize(
						new MobileResponse()
						{
							AppId = config.AppId,
							Uuid = pxContext.UUID,
							Action = pxContext.MapBlockAction(),
							Vid = pxContext.Vid,
							Page = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
							CollectorUrl = string.Format(config.CollectorUrl, config.AppId)
						}, output);
					content = output.ToString();
				}

				pxContext.ApplicationContext.Response.Write(content);
				return;
			}

			// json response
			if (IsJsonResponse(pxContext))
			{
				pxContext.ApplicationContext.Response.ContentType = "application/json";
				using (var output = new StringWriter())
				{
					var props = TemplateFactory.getProps(config, pxContext.UUID, pxContext.Vid, pxContext.IsMobileRequest, pxContext.BlockAction);
					JSON.Serialize(
						new JsonResponse()
						{
							AppId = config.AppId,
							Uuid = pxContext.UUID,
							Vid = pxContext.Vid,
							JsClientSrc = props["jsClientSrc"],
							HostUrl = props["hostUrl"],
							BlockScript = props["blockScript"]
						}, output);
					content = output.ToString();
				}

				pxContext.ApplicationContext.Response.Write(content);
				return;
			}

			if (pxContext.BlockAction != "c" && pxContext.BlockAction != "b")
			{
				if (pxContext.BlockAction == "r")
				{
					pxContext.ApplicationContext.Response.StatusCode = 429; // HTTP/1.1 429 TooManyRequests
				}

				pxContext.ApplicationContext.Response.Write(content);
				return;
			}

			if (pxContext.CustomBlockUrl != "")
			{
				if (pxContext.RedirectOnCustomUrl)
				{
					string uri = pxContext.ApplicationContext.Request.Url.AbsoluteUri;
					string encodedUri = Convert.ToBase64String(Encoding.UTF8.GetBytes(uri));
					string redirectUrl = string.Format("{0}?url={1}&uuid={2}&vid={3}", pxContext.CustomBlockUrl, encodedUri, pxContext.UUID, pxContext.Vid);
					PxLoggingUtils.LogDebug("Redirecting to custom block page: " + redirectUrl);
					pxContext.ApplicationContext.Response.Redirect(redirectUrl);
					return;
				}

				HttpResponseMessage response = httpClient.GetAsync(pxContext.CustomBlockUrl).Result;
				if ((int)response.StatusCode >= 300)
				{
					pxContext.ApplicationContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
					pxContext.ApplicationContext.Response.Write("Unable to fetch custom block url. Status: " + response.StatusCode.ToString());
					return;
				}

				content = response.Content.ReadAsStringAsync().Result;
				if (pxContext.BlockAction == "c")
				{
					PxLoggingUtils.LogDebug("Injecting captcha to page");
					StringBuilder builder = new StringBuilder(content);
					builder.Replace("</head>", injectCaptchaScript(pxContext.Vid, pxContext.UUID) + "</head>");
					builder.Replace("::BLOCK_REF::", pxContext.UUID);
					content = builder.ToString();
				}

				pxContext.ApplicationContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
				pxContext.ApplicationContext.Response.Write(content);
				return;
			}

			PxLoggingUtils.LogDebug("Enforcing action: " + pxContext.MapBlockAction() + " page is served");
			pxContext.ApplicationContext.Response.Write(content);
		}
	}
}
