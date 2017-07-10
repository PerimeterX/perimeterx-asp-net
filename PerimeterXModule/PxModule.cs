// 	Copyright � 2016 PerimeterX, Inc.
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
using System.Net.Http;
using System.Text;
using System.IO;
using System.Net.Http.Headers;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Timers;

namespace PerimeterX
{
	public class PxModule : IHttpModule
	{
		private static IActivityReporter reporter;
        private static RemoteConfigurationManager remoteConfigurationManager;
        private static PXConfigurationWrapper pxConfig;
        private static TimerConfigUpdater timerConfigUpdater;
		private static HttpClient httpClient;
        private static DefaultPxClient pxClient;

        private PxContext pxContext;
		private readonly string validationMarker;
        private readonly ICookieDecoder cookieDecoder;
        private readonly IPXCaptchaValidator pxCaptchaValidator;
        private readonly IPXCookieValidator pxCookieValidator;
        private readonly IPXS2SValidator pxS2SValidator;
        private readonly VerificationHandler verificationHandler;

        // Set here everything that need to have single instance/Singleton
		static PxModule()
		{
			try
			{
                Debug.WriteLine("PerimeterX Static block executed");
                var moduleConfiguration = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);
				// allocate reporter if needed
				pxConfig = new PXConfigurationWrapper(moduleConfiguration);
                if (moduleConfiguration != null && (moduleConfiguration.SendBlockActivites || moduleConfiguration.SendPageActivites))
                {
					reporter = new ActivityReporter(PxConstants.FormatBaseUri(pxConfig), pxConfig.ActivitiesCapacity, pxConfig.ActivitiesBulkSize, pxConfig.ReporterApiTimeout);
				}
				else
				{
					reporter = new NullActivityMonitor();
				}

				var pxDefaultClient = new DefaultPxClient(pxConfig);

                if (pxConfig.RemoteConfigurationEnabled)
				{
                    remoteConfigurationManager = new DefaultRemoteConfigurationManager(pxConfig, pxDefaultClient);
                    timerConfigUpdater = new TimerConfigUpdater(remoteConfigurationManager);
                    timerConfigUpdater.Schedule();
				}

			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to extract assembly version " + ex.Message, PxConstants.LOG_CATEGORY);
			}
		}

		public PxModule()
		{
            Debug.WriteLine("PerimeterX Module Initalize");

			// Set Decoder
			if (pxConfig.EncryptionEnabled)
			{
				cookieDecoder = new EncryptedCookieDecoder(pxConfig);
			}
			else
			{
				cookieDecoder = new CookieDecoder();
			}
			
			using (var hasher = new SHA256Managed())
			{
				validationMarker = ByteArrayToHexString(hasher.ComputeHash(Encoding.UTF8.GetBytes(pxConfig.CookieKey)));
			}

            // Set Validators
			pxClient = new DefaultPxClient(pxConfig);
            pxS2SValidator = new PXS2SValidator(pxConfig, pxClient);
            pxCaptchaValidator = new PXCaptchaValidator(pxConfig, pxClient);
            pxCookieValidator = new PXCookieValidator(pxConfig);
            verificationHandler = new DefaultVerificationHandler(reporter);

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

                VerifyRequest(application);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to validate request: " + ex.Message, PxConstants.LOG_CATEGORY);
			}
		}

        private bool VerifyRequest(HttpApplication application)
		{
			try
			{
                pxContext = new PxContext(application.Context, pxConfig);

				// check captcha after cookie validation to capture vid
				if (!string.IsNullOrEmpty(pxContext.PxCaptcha) && pxCaptchaValidator.CaptchaVerify(pxContext))
				{
					return true;
				}

				// validate using risk cookie
				IPxCookie pxCookie = PxCookieUtils.BuildCookie(pxConfig, pxContext, cookieDecoder);
				if (!pxCookieValidator.CookieVerify(pxContext, pxCookie))
				{
					// validate using server risk api
					pxS2SValidator.VerifyS2S(pxContext);
				}

                return verificationHandler.HandleVerificatoin(pxConfig, pxContext, application);
			}
			catch (Exception ex)
			{
                Debug.WriteLine("Module failed to process request in fault: {0}, passing request", ex.Message, PxConstants.LOG_CATEGORY);
				pxContext.PassReason = PassReasonEnum.ERROR;
                return verificationHandler.HandleVerificatoin(pxConfig, pxContext, application);
			}
		}

		public void Dispose()
		{
            Debug.WriteLine("Shutting down Px Module");
			if (httpClient != null)
			{
				httpClient.Dispose();
				httpClient = null;
			}
		}

		private bool IsFilteredRequest(HttpContext context)
		{
			if (!pxConfig.Enabled)
			{
				return true;
			}


			// whitelist file extension
			var ext = Path.GetExtension(context.Request.Url.AbsolutePath).ToLowerInvariant();
			if (pxConfig.FileExtWhitelist != null && pxConfig.FileExtWhitelist.Contains(ext))
			{
				return true;
			}

			// whitelist routes prefix
			var url = context.Request.Url.AbsolutePath;
			if (pxConfig.RoutesWhitelist != null)
			{
				foreach (var prefix in pxConfig.RoutesWhitelist)
				{
					if (url.StartsWith(prefix))
					{
						return true;
					}
				}
			}

			// whitelist user-agent
			if (pxConfig.UseragentsWhitelist != null && pxConfig.UseragentsWhitelist.Contains(context.Request.UserAgent))
			{
				return true;
			}

			return false;
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
