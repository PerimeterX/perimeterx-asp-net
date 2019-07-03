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
using System.Text;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections;
using Jil;
using System.Collections.Generic;
using PerimeterX.Internals;
using System.Web.Script.Serialization;

namespace PerimeterX
{
	public class PxModule : IHttpModule
	{
		private HttpHandler httpHandler;
		private PxContext pxContext;
		private static IActivityReporter reporter;
		private readonly string validationMarker;
		private readonly ICookieDecoder cookieDecoder;
		private readonly IPXCookieValidator PxCookieValidator;
		private readonly IPXS2SValidator PxS2SValidator;
		private readonly IReverseProxy ReverseProxy;
		private readonly PxBlock pxBlock;

		private readonly bool enabled;
		private readonly bool sendPageActivites;
		private readonly bool sendBlockActivities;
		private readonly int blockingScore;
		private readonly string appId;
		private readonly IVerificationHandler customVerificationHandlerInstance;
		private readonly bool suppressContentBlock;
		private readonly bool challengeEnabled;
		private readonly string[] sensetiveHeaders;
		private readonly StringCollection fileExtWhitelist;
		private readonly StringCollection routesWhitelist;
		private readonly StringCollection useragentsWhitelist;
		private readonly StringCollection enforceSpecificRoutes;
		private readonly string cookieKey;
		private readonly string customBlockUrl;
		private readonly byte[] cookieKeyBytes;
		private readonly string osVersion;
		private string nodeName;

		static PxModule()
		{
			try
			{
				var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);
				PxLoggingUtils.init(config.AppId);
				// allocate reporter if needed
				if (config != null && (config.SendBlockActivites || config.SendPageActivites))
				{
					reporter = new ActivityReporter(PxConstants.FormatBaseUri(config), config.ActivitiesCapacity, config.ActivitiesBulkSize, config.ReporterApiTimeout);
				}
				else
				{
					reporter = new NullActivityMonitor();
				}
			}
			catch (Exception ex)
			{
				PxLoggingUtils.LogDebug("Failed to extract assembly version " + ex.Message);
			}
		}

		public PxModule()
		{
			var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);
			if (config == null)
			{
				throw new ConfigurationErrorsException("Missing PerimeterX module configuration section " + PxConstants.CONFIG_SECTION);
			}

			// load configuration
			enabled = config.Enabled;
			sendPageActivites = config.SendPageActivites;
			sendBlockActivities = config.SendBlockActivites;
			cookieKey = config.CookieKey;
			cookieKeyBytes = Encoding.UTF8.GetBytes(cookieKey);
			blockingScore = config.BlockingScore;
			appId = config.AppId;
			customVerificationHandlerInstance = GetCustomVerificationHandler(config.CustomVerificationHandler);
			suppressContentBlock = config.SuppressContentBlock;
			challengeEnabled = config.ChallengeEnabled;
			sensetiveHeaders = config.SensitiveHeaders.Cast<string>().ToArray();
			fileExtWhitelist = config.FileExtWhitelist;
			routesWhitelist = config.RoutesWhitelist;
			useragentsWhitelist = config.UseragentsWhitelist;
			enforceSpecificRoutes = config.EnforceSpecificRoutes;

			// Set Decoder
			if (config.EncryptionEnabled)
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

			this.httpHandler = new HttpHandler(config, PxConstants.FormatBaseUri(config), config.ApiTimeout);

			// Set Validators
			PxS2SValidator = new PXS2SValidator(config, httpHandler);
			PxCookieValidator = new PXCookieValidator(config)
			{
				PXOriginalTokenValidator = new PXOriginalTokenValidator(config)
			};

			// Get OS type
			osVersion = Environment.OSVersion.VersionString;

			// Build reverse proxy
			ReverseProxy = new ReverseProxy(config);

			pxBlock = new PxBlock(config);

			PxLoggingUtils.LogDebug(ModuleName + " initialized");
		}

		public string ModuleName
		{
			get { return "PxModule"; }
		}

		public void Init(HttpApplication application)
		{
			application.BeginRequest += this.Application_BeginRequest;
			nodeName = application.Context.Server.MachineName;
		}

		private void Application_BeginRequest(object source, EventArgs e)
		{
			try
			{
				var application = (HttpApplication)source;

				if (application == null)
				{
					return;
				}

				var applicationContext = application.Context;

				if (applicationContext == null || IsFirstPartyProxyRequest(applicationContext))
				{
					return;
				}

				if (IsFilteredRequest(applicationContext))
				{
					return;
				}
				try
				{
					//check if this is a telemetry command
					if (IsTelemetryCommand(applicationContext))
					{
						//command is valid. send telemetry
						PostEnforcerTelemetryActivity();
					}
				}
				catch (Exception ex)
				{
					PxLoggingUtils.LogDebug("Failed to validate Telemetry command request: " + ex.Message);
				}

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
				PxLoggingUtils.LogDebug("Failed to validate request: " + ex.Message);
			}
		}

		private bool IsTelemetryCommand(HttpContext applicationContext)
		{
			//extract header value and decode it from base64 string
			string headerValue = applicationContext.Request.Headers[PxConstants.ENFORCER_TELEMETRY_HEADER];
			if(headerValue == null)
			{
				return false;
			}

			//we got Telemetry command request
			PxLoggingUtils.LogDebug("Received command to send enforcer telemetry");

			//base 64 decode
			string decodedString = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue));

			//value is in the form of timestamp:hmac_val
			string[] splittedValue = decodedString.Split(new Char[] { ':' });
			if (splittedValue.Length != 2)
			{
				PxLoggingUtils.LogDebug("Malformed header value - " + PxConstants.ENFORCER_TELEMETRY_HEADER + " = " + headerValue);
				return false;
			}

			//timestamp
			DateTime expirationTime = (new DateTime(1970, 1, 1)).AddMilliseconds(double.Parse(splittedValue[0]));
			if ((expirationTime - DateTime.UtcNow).Ticks < 0)
			{
				//commmand is expired
				PxLoggingUtils.LogDebug("Telemetry command is expired");
				return false;
			}

			//check hmac integrity
			string generatedHmac = BitConverter.ToString(new HMACSHA256(cookieKeyBytes).ComputeHash(Encoding.UTF8.GetBytes(splittedValue[0]))).Replace("-", "");
			if (generatedHmac != splittedValue[1].ToUpper())
			{
				PxLoggingUtils.LogDebug("hmac validation failed. original = " + splittedValue[1] + ", generated = " + generatedHmac);
				return false;
			}

			return true;
		}

		private void PostPageRequestedActivity(PxContext pxContext)
		{
			if (sendPageActivites)
			{
				PostActivity(pxContext, "page_requested", new ActivityDetails
				{
					ModuleVersion = PxConstants.MODULE_VERSION,
					PassReason = pxContext.PassReason,
					RiskRoundtripTime = pxContext.RiskRoundtripTime,
					ClientUuid = pxContext.UUID
				});
			}
		}

		private void PostBlockActivity(PxContext pxContext)
		{
			if (sendBlockActivities)
			{
				PostActivity(pxContext, "block", new ActivityDetails
				{
					BlockAction = pxContext.BlockAction,
					BlockReason = pxContext.BlockReason,
					BlockUuid = pxContext.UUID,
					ModuleVersion = PxConstants.MODULE_VERSION,
					RiskScore = pxContext.Score,
					RiskRoundtripTime = pxContext.RiskRoundtripTime
				});
			}
		}

		private void PostEnforcerTelemetryActivity()
		{
			var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);

			string serializedConfig;
			using (var json = new StringWriter())
			{
				JSON.Serialize(config, json);
				serializedConfig = json.ToString();
			}

			//remove cookieKey and ApiToken from telemetry
			var jsSerializer = new JavaScriptSerializer();
			Dictionary<string, object> dict = (Dictionary<string, object>)jsSerializer.DeserializeObject(serializedConfig);
			dict.Remove("CookieKey");
			dict.Remove("ApiToken");
			serializedConfig = new JavaScriptSerializer().Serialize(dict);

			var activity = new Activity
			{
				Type = "enforcer_telemetry",
				Timestamp = PxConstants.GetTimestamp(),
				AppId = appId,
				Details = new EnforcerTelemetryActivityDetails
				{
					ModuleVersion = PxConstants.MODULE_VERSION,
					UpdateReason = EnforcerTelemetryUpdateReasonEnum.COMMAND,
					OsName = osVersion,
					NodeName = nodeName,
					EnforcerConfigs = serializedConfig
				}
			};

			try
			{
				var stringBuilder = new StringBuilder();
				using (var stringOutput = new StringWriter(stringBuilder))
				{
					JSON.SerializeDynamic(activity, stringOutput, Options.ExcludeNullsIncludeInherited);
				}

				httpHandler.Post(stringBuilder.ToString(), PxConstants.ENFORCER_TELEMETRY_API_PATH);
			}
			catch (Exception ex)
			{
				PxLoggingUtils.LogDebug(string.Format("Encountered an error sending enforcer telemetry activity: {0}.", ex.Message));
			}
		}

		private void PostActivity(PxContext pxContext, string eventType, ActivityDetails details = null)
		{
			var activity = new Activity
			{
				Type = eventType,
				Timestamp = PxConstants.GetTimestamp(),
				AppId = appId,
				SocketIP = pxContext.Ip,
				Url = pxContext.FullUrl,
				Details = details,
				Headers = pxContext.GetHeadersAsDictionary(),
			};
			if (eventType.Equals("page_requested"))
			{
				activity.HttpMethod = "Post";
			}

			if (!string.IsNullOrEmpty(pxContext.Vid))
			{
				activity.Vid = pxContext.Vid;
			}

			if (!string.IsNullOrEmpty(pxContext.Pxhd) && (eventType == "page_requested" || eventType == "block"))
			{
				activity.Pxhd = pxContext.Pxhd;
			}

			reporter.Post(activity);
		}

		public void BlockRequest(PxContext pxContext, PxModuleConfigurationSection config)
		{
			pxContext.ApplicationContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
			pxContext.ApplicationContext.Response.TrySkipIisCustomErrors = true;
			if (config.SuppressContentBlock)
			{
				pxContext.ApplicationContext.Response.SuppressContent = true;
			}
			else
			{
				pxBlock.ResponseBlockPage(pxContext, config);
			}
		}

		public void Dispose()
		{
			if (httpHandler != null)
			{
				httpHandler.Dispose();
			}
		}


		/// <summary>
		/// Checks the url if it should be a proxy request for the client/xhrs
		/// </summary>
		/// <param name="context">HTTP context for client</param>
		private bool IsFirstPartyProxyRequest(HttpContext context)
		{
			return ReverseProxy.ShouldReverseClient(context) || ReverseProxy.ShouldReverseCaptcha(context) || ReverseProxy.ShouldReverseXhr(context);
		}

		private bool IsFilteredRequest(HttpContext context)
		{
			if (!enabled)
			{
				return true;
			}


			// whitelist file extension
			var ext = Path.GetExtension(context.Request.Url.AbsolutePath).ToLowerInvariant();
			if (fileExtWhitelist != null && fileExtWhitelist.Contains(ext))
			{
				return true;
			}

			var url = context.Request.Url.AbsolutePath;

			// custom block url check
			if (customBlockUrl != null && url == customBlockUrl) {
				return true;
			}

			// whitelist routes prefix
			if (routesWhitelist != null)
			{
				foreach (var prefix in routesWhitelist)
				{
					if (url.StartsWith(prefix))
					{
						return true;
					}
				}
			}

			// whitelist user-agent
			if (useragentsWhitelist != null && useragentsWhitelist.Contains(context.Request.UserAgent))
			{
				return true;
			}

			// enforce specific routes prefix
			if (enforceSpecificRoutes != null)
			{
				// case list is not empty, module will skip the route if
				// the routes prefix is not present in the list
				foreach (var prefix in enforceSpecificRoutes)
				{
					if (url.StartsWith(prefix))
					{
						return false;
					}
				}
				// we go over all the list and prefix wasn't found
				// meaning this route is not a specifc route
				return true;
			}

			return false;
		}

		private void VerifyRequest(HttpApplication application)
		{
			try
			{
				var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);
				pxContext = new PxContext(application.Context, config);

				// validate using risk cookie
				IPxCookie pxCookie = PxCookieUtils.BuildCookie(config, pxContext.PxCookies, cookieDecoder);
				pxContext.OriginalToken = PxCookieUtils.BuildCookie(config, pxContext.OriginalTokens, cookieDecoder);
				if (!PxCookieValidator.Verify(pxContext, pxCookie))
				{
					// validate using server risk api
					PxS2SValidator.VerifyS2S(pxContext);
				}

				HandleVerification(application);
			}
			catch (Exception ex) // Fail-open approach
			{
				PxLoggingUtils.LogError(string.Format("Module failed to process request in fault: {0}, passing request", ex.Message));
				pxContext.PassReason = PassReasonEnum.ERROR;
				PostPageRequestedActivity(pxContext);
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

		private void HandleVerification(HttpApplication application)
		{
			PxModuleConfigurationSection config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);
			bool verified = blockingScore > pxContext.Score;

			PxLoggingUtils.LogDebug(string.Format("Request score: {0}, blocking score: {1}, monitor mode status: {2}.", pxContext.Score, blockingScore, config.MonitorMode == true ? "On" : "Off"));

			if (verified)
			{
				if (config.MonitorMode)
				{
					PxLoggingUtils.LogDebug("Monitor Mode is activated. passing request");
				}
				PxLoggingUtils.LogDebug(string.Format("Valid request to {0}", application.Context.Request.RawUrl));
				PostPageRequestedActivity(pxContext);
			}
			else
			{
				PxLoggingUtils.LogDebug(string.Format("Invalid request to {0}", application.Context.Request.RawUrl));
				PostBlockActivity(pxContext);
			}

			SetPxhdAndVid(pxContext);
			// If implemented, run the customVerificationHandler.
			if (customVerificationHandlerInstance != null)
			{
				customVerificationHandlerInstance.Handle(application, pxContext, config);
			}
			// No custom verification handler -> continue regular flow
			else if (!verified && !pxContext.MonitorRequest)
			{
				BlockRequest(pxContext, config);
				application.CompleteRequest();
			}
		}

		private static void SetPxhdAndVid(PxContext pxContext)
		{

			if (!string.IsNullOrEmpty(pxContext.Pxhd))
			{
				string pxhd = PxConstants.COOKIE_PXHD_PREFIX + "=" + pxContext.Pxhd + "; path=/";
				pxContext.ApplicationContext.Response.AddHeader("Set-Cookie", pxhd);
			}
		}

		/// <summary>
		/// Uses reflection to check whether an IVerificationHandler was implemented by the customer.
		/// </summary>
		/// <returns>If found, returns the IVerificationHandler class instance. Otherwise, returns null.</returns>
		private static IVerificationHandler GetCustomVerificationHandler(string customHandlerName)
		{
			if (string.IsNullOrEmpty(customHandlerName))
			{
				return null;
			}

			try
			{
				var customVerificationHandlerType =
					AppDomain.CurrentDomain.GetAssemblies()
							 .SelectMany(a => {
								 try
								 {
									 return a.GetTypes();
								 }
								 catch
								 {
									 return new Type[0];
								 }
							 })
							 .FirstOrDefault(t => t.GetInterface(typeof(IVerificationHandler).Name) != null &&
												  t.Name.Equals(customHandlerName) && t.IsClass && !t.IsAbstract);

				if (customVerificationHandlerType != null)
				{
					var instance = (IVerificationHandler)Activator.CreateInstance(customVerificationHandlerType, null);
					PxLoggingUtils.LogDebug(string.Format("Successfully loaded ICustomeVerificationHandler '{0}'.", customHandlerName));
					return instance;
				}
				else
				{
					PxLoggingUtils.LogDebug(string.Format(
						"Missing implementation of the configured IVerificationHandler ('customVerificationHandler' attribute): {0}.",
						customHandlerName));
				}
			}
			catch (ReflectionTypeLoadException ex)
			{
				PxLoggingUtils.LogError(string.Format("Failed to load the ICustomeVerificationHandler '{0}': {1}.",
											  customHandlerName, ex.Message));
			}
			catch (Exception ex)
			{
				PxLoggingUtils.LogError(string.Format("Encountered an error while retrieving the ICustomeVerificationHandler '{0}': {1}.",
											  customHandlerName, ex.Message));
			}

			return null;
		}
	}
}
