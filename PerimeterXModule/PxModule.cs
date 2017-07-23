﻿// 	Copyright � 2016 PerimeterX, Inc.
//
// Permission is hereby granted, free of charge, to any
// person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the
// Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice
// shall be included in all copies or substantial portions of
// the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
// KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFINGEMENT. IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
// OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Web;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Collections;

namespace PerimeterX
{
	public class PxModule : IHttpModule
	{
		private static IActivityReporter reporter;
		private static IPXConfiguration pxConfiguration;

		private PxContext pxContext;

		private readonly string validationMarker;
		private readonly ICookieDecoder cookieDecoder;

		private readonly IPXCaptchaValidator PxCaptchaValidator;
		private readonly IPXCookieValidator PxCookieValidator;
		private readonly IPXS2SValidator PxS2SValidator;

		static PxModule()
		{
			try
			{
				var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);
				if (config == null)
				{
					throw new ConfigurationErrorsException("Missing PerimeterX module configuration section " + PxConstants.CONFIG_SECTION);
				}

				pxConfiguration = ConfigurationFactory.Create(config);

				// allocate reporter if needed
				if (pxConfiguration != null && (pxConfiguration.SendBlockActivites || pxConfiguration.SendPageActivites))
				{
					reporter = new ActivityReporter(PxConstants.FormatBaseUri(pxConfiguration), pxConfiguration.ActivitiesCapacity, pxConfiguration.ActivitiesBulkSize, pxConfiguration.ReporterApiTimeout);
				}
				else
				{
					reporter = new NullActivityMonitor();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to extract assembly version " + ex.Message, PxConstants.LOG_CATEGORY);
			}
		}

		public PxModule()
		{
			byte[] cookieKeyBytes = Encoding.UTF8.GetBytes(pxConfiguration.CookieKey);
			// Set Decoder
			if (pxConfiguration.EncryptionEnabled)
			{
				cookieDecoder = new EncryptedCookieDecoder(cookieKeyBytes);
			}
			else
			{
				cookieDecoder = new CookieDecoder();
			}

			using (var hasher = new SHA256Managed())
			{
				validationMarker = ByteArrayToHexString(hasher.ComputeHash(cookieKeyBytes));
			}

			// Set Validators
			IPXHttpClient httpClient = new PXHttpClient(pxConfiguration);

			PxS2SValidator = new PXS2SValidator(pxConfiguration, httpClient);
			PxCaptchaValidator = new PXCaptchaValidator(pxConfiguration, httpClient);
			PxCookieValidator = new PXCookieValidator(pxConfiguration);

			Debug.WriteLine(ModuleName + " initialized", PxConstants.LOG_CATEGORY);
		}

		public string ModuleName
		{
			get { return "PxModule"; }
		}

		public void Init(HttpApplication application)
		{
			application.BeginRequest += this.Application_BeginRequest;
		}

		private void Application_BeginRequest(object source, EventArgs e)
		{
			try
			{
				var application = (HttpApplication)source;
				if (application == null || IsFilteredRequest(application.Context))
				{
					return;
				}
				var applicationContext = application.Context;
				if (validationMarker == applicationContext.Request.Headers[PxConstants.PX_VALIDATED_HEADER])
				{
					return;
				}
				// Setting custom header for classic mode
				if (HttpRuntime.UsingIntegratedPipeline)
				{
					applicationContext.Request.Headers.Add(PxConstants.PX_VALIDATED_HEADER, validationMarker);

				}
				else
				{
					var headers = applicationContext.Request.Headers;
					Type hdr = headers.GetType();
					PropertyInfo ro = hdr.GetProperty("IsReadOnly",
						BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
					// Remove the ReadOnly property
					ro.SetValue(headers, false, null);
					// Invoke the protected InvalidateCachedArrays method 
					hdr.InvokeMember("InvalidateCachedArrays",
						BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
						null, headers, null);
					// Now invoke the protected "BaseAdd" method of the base class to add the
					// headers you need. The header content needs to be an ArrayList or the
					// the web application will choke on it.
					hdr.InvokeMember("BaseAdd",
						BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Instance,
						null, headers,
						new object[] { PxConstants.PX_VALIDATED_HEADER, new ArrayList { validationMarker } });
					// repeat BaseAdd invocation for any other headers to be added
					// Then set the collection back to ReadOnly
					ro.SetValue(headers, true, null);
				}

				if (VerifyRequest(applicationContext))
				{
					Debug.WriteLine("Valid request to " + applicationContext.Request.RawUrl, PxConstants.LOG_CATEGORY);
					PostPageRequestedActivity(pxContext);

				}
				else
				{
					Debug.WriteLine("Invalid request to " + applicationContext.Request.RawUrl, PxConstants.LOG_CATEGORY);
					PostBlockActivity(pxContext);
					BlockRequest(pxContext);
					application.CompleteRequest();
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to validate request: " + ex.Message, PxConstants.LOG_CATEGORY);
			}
		}

		private void PostPageRequestedActivity(PxContext pxContext)
		{
			if (pxConfiguration.SendPageActivites)
			{
				PostActivity(pxContext, "page_requested", new ActivityDetails
				{
					ModuleVersion = PxConstants.MODULE_VERSION,
					PassReason = pxContext.PassReason,
					RiskRoundtripTime = pxContext.RiskRoundtripTime
				});
			}
		}

		private void PostBlockActivity(PxContext pxContext)
		{
			if (pxConfiguration.SendBlockActivites)
			{
				PostActivity(pxContext, "block", new ActivityDetails
				{
					BlockReason = pxContext.BlockReason,
					BlockUuid = pxContext.UUID,
					ModuleVersion = PxConstants.MODULE_VERSION,
					RiskScore = pxContext.Score,
					RiskRoundtripTime = pxContext.RiskRoundtripTime
				});
			}
		}

		private void PostActivity(PxContext pxContext, string eventType, ActivityDetails details = null)
		{
			var activity = new Activity
			{
				Type = eventType,
				Timestamp = Math.Round(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, MidpointRounding.AwayFromZero),
				AppId = pxConfiguration.AppId,
				SocketIP = pxContext.Ip,
				Url = pxContext.FullUrl,
				Details = details
			};

			activity.Headers = new Dictionary<string, string>();

			foreach (RiskRequestHeader riskHeader in pxContext.Headers)
			{
				var key = riskHeader.Name;
				activity.Headers.Add(key, riskHeader.Value);
			}

			reporter.Post(activity);
		}

		private void BlockRequest(PxContext pxContext)
		{
			pxContext.ApplicationContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
			pxContext.ApplicationContext.Response.TrySkipIisCustomErrors = true;
			if (pxConfiguration.SuppressContentBlock)
			{
				pxContext.ApplicationContext.Response.SuppressContent = true;
			}
			else
			{
				ResponseBlockPage(pxContext);
			}
		}

		private void ResponseBlockPage(PxContext pxContext)
		{
			string template = "block";
			string content;
			if (pxContext.BlockAction.Equals("c"))
			{
				template = "captcha";
			}
			Debug.WriteLine(string.Format("Using {0} template", template), PxConstants.LOG_CATEGORY);
			content = TemplateFactory.getTemplate(template, pxConfiguration, pxContext.UUID, pxContext.Vid);
			pxContext.ApplicationContext.Response.Write(content);
		}

		public void Dispose()
		{

		}

		private bool IsFilteredRequest(HttpContext context)
		{
			if (!pxConfiguration.Enabled)
			{
				return true;
			}


			// whitelist file extension
			var ext = Path.GetExtension(context.Request.Url.AbsolutePath).ToLowerInvariant();
			if (pxConfiguration.FileExtWhitelist != null && pxConfiguration.FileExtWhitelist.Contains(ext))
			{
				return true;
			}

			// whitelist routes prefix
			var url = context.Request.Url.AbsolutePath;
			if (pxConfiguration.RoutesWhitelist != null)
			{

				foreach (var prefix in pxConfiguration.RoutesWhitelist)
				{
					if (url.StartsWith(prefix))
					{
						return true;
					}
				}
			}

			// whitelist user-agent
			if (pxConfiguration.UseragentsWhitelist != null && pxConfiguration.UseragentsWhitelist.Contains(context.Request.UserAgent))
			{
				return true;
			}

			return false;
		}

		private bool VerifyRequest(HttpContext applicationContext)
		{
			try
			{
				pxContext = new PxContext(applicationContext, pxConfiguration);

				// check captcha after cookie validation to capture vid
				if (!string.IsNullOrEmpty(pxContext.PxCaptcha) && PxCaptchaValidator.CaptchaVerify(pxContext))
				{
					return true;
				}

				// validate using risk cookie
				IPxCookie pxCookie = PxCookieUtils.BuildCookie(pxConfiguration, pxContext, cookieDecoder);
				if (!PxCookieValidator.CookieVerify(pxContext, pxCookie))
				{
					// validate using server risk api
					PxS2SValidator.VerifyS2S(pxContext);
				}

				return pxConfiguration.BlockingScore > pxContext.Score;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Module failed to process request in fault: {0}, passing request", ex.Message, PxConstants.LOG_CATEGORY);
				pxContext.PassReason = PassReasonEnum.ERROR;
				return true; //true pass request
			}
		}

		private static string ByteArrayToHexString(byte[] input)
		{
			StringBuilder sb = new StringBuilder(input.Length * 2);
			foreach (byte b in input)
			{
				sb.Append(PxConstants.HEX_ALPHABET[b >> 4]);
				sb.Append(PxConstants.HEX_ALPHABET[b & 0xF]);
			}
			return sb.ToString();
		}

	}
}
