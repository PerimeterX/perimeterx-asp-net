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
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Http.Headers;
using System.Configuration;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace PerimeterX
{
    public class PxModule : IHttpModule
    {
        private HttpClient httpClient;
        private PxContext pxContext;
        private static IActivityReporter reporter;
        private readonly string validationMarker;
        private readonly ICookieDecoder cookieDecoder;
        private readonly IPXCaptchaValidator PxCaptchaValidator;
        private readonly IPXCookieValidator PxCookieValidator;
        private readonly IPXS2SValidator PxS2SValidator;

        private readonly bool enabled;
        private readonly bool sendPageActivites;
        private readonly bool sendBlockActivities;
        private readonly int blockingScore;
        private readonly string appId;
        private readonly string customVerificationHandler;
        private readonly bool suppressContentBlock;
        private readonly bool challengeEnabled;
        private readonly string captchaProvider;
        private readonly string[] sensetiveHeaders;
        private readonly StringCollection fileExtWhitelist;
        private readonly StringCollection routesWhitelist;
        private readonly StringCollection useragentsWhitelist;
        private readonly string cookieKey;
        private readonly byte[] cookieKeyBytes;

        static PxModule()
        {
            try
            {
                var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);
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
                Debug.WriteLine("Failed to extract assembly version " + ex.Message, PxConstants.LOG_CATEGORY);
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
            customVerificationHandler = config.CustomVerificationHandler;
            suppressContentBlock = config.SuppressContentBlock;
            captchaProvider = config.CaptchaProvider;
            challengeEnabled = config.ChallengeEnabled;

            sensetiveHeaders = config.SensitiveHeaders.Cast<string>().ToArray();
            fileExtWhitelist = config.FileExtWhitelist;
            routesWhitelist = config.RoutesWhitelist;
            useragentsWhitelist = config.UseragentsWhitelist;

            // Set Decoder
            if (config.EncryptionEnabled)
            {
                cookieDecoder = new EncryptedCookieDecoder(cookieKeyBytes);
            }
            else
            {
                cookieDecoder = new CookieDecoder();
            }

            var webRequestHandler = new WebRequestHandler
            {
                AllowPipelining = true,
                UseDefaultCredentials = true,
                UnsafeAuthenticatedConnectionSharing = true
            };
            this.httpClient = new HttpClient(webRequestHandler, true)
            {
                Timeout = TimeSpan.FromMilliseconds(config.ApiTimeout)
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.ExpectContinue = false;

            using (var hasher = new SHA256Managed())
            {
                validationMarker = ByteArrayToHexString(hasher.ComputeHash(cookieKeyBytes));
            }

            // Set Validators

            PxS2SValidator = new PXS2SValidator(config, httpClient);
            PxCaptchaValidator = new PXCaptchaValidator(config, httpClient);
            PxCookieValidator = new PXCookieValidator(config);

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

        private void PostPageRequestedActivity(PxContext pxContext)
        {
            if (sendPageActivites)
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
            if (sendBlockActivities)
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
                AppId = appId,
                SocketIP = pxContext.Ip,
                Url = pxContext.FullUrl,
                Details = details,
                Headers = pxContext.GetHeadersAsDictionary()
            };

            reporter.Post(activity);
        }

        public static void BlockRequest(PxContext pxContext, PxModuleConfigurationSection config)
        {
            pxContext.ApplicationContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            pxContext.ApplicationContext.Response.TrySkipIisCustomErrors = true;
            if (config.SuppressContentBlock)
            {
                pxContext.ApplicationContext.Response.SuppressContent = true;
            }
            else
            {
                ResponseBlockPage(pxContext, config);
            }
        }

        public static void ResponseBlockPage(PxContext pxContext, PxModuleConfigurationSection config)
        {
            string template = "block";
            string content;

            if (pxContext.BlockAction == "j")
            {
                template = "challenge";
            }
            else if (pxContext.BlockAction == "c")
            {
                template = config.CaptchaProvider;
            }

            Debug.WriteLine(string.Format("Using {0} template", template), PxConstants.LOG_CATEGORY);

            // In the case of a challenge, the challenge response is taken directly from BlockData. Otherwise, generate html template.
            content = template == "challenge" && !string.IsNullOrEmpty(pxContext.BlockData) ? pxContext.BlockData :
                TemplateFactory.getTemplate(template, config, pxContext.UUID, pxContext.Vid);

            pxContext.ApplicationContext.Response.Write(content);
        }

        public void Dispose()
        {
            if (httpClient != null)
            {
                httpClient.Dispose();
                httpClient = null;
            }
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

            // whitelist routes prefix
            var url = context.Request.Url.AbsolutePath;
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

            return false;
        }

        private void VerifyRequest(HttpApplication application)
        {
            try
            {
                var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);
                pxContext = new PxContext(application.Context, config);

                // check captcha after cookie validation to capture vid
                if (!string.IsNullOrEmpty(pxContext.PxCaptcha) && PxCaptchaValidator.CaptchaVerify(pxContext))
                {
                    HandleVerification(application);
                    return;
                }

                // validate using risk cookie
                IPxCookie pxCookie = PxCookieUtils.BuildCookie(config, pxContext, cookieDecoder);
                if (!PxCookieValidator.CookieVerify(pxContext, pxCookie))
                {
                    // validate using server risk api
                    PxS2SValidator.VerifyS2S(pxContext);
                }

                HandleVerification(application);
            }
            catch (Exception ex) // Fail-open approach
            {
                Debug.WriteLine(string.Format("Module failed to process request in fault: {0}, passing request", ex.Message), PxConstants.LOG_CATEGORY);
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
            bool verified = blockingScore > pxContext.Score;
            PxModuleConfigurationSection config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(PxConstants.CONFIG_SECTION);

            Debug.WriteLine(string.Format("Request score: {0}, blocking score: {1}, monitor mode status: {2}.", pxContext.Score, blockingScore, config.MonitorMode == true ? "On" : "Off"), PxConstants.LOG_CATEGORY);

            if (verified)
            {
                Debug.WriteLine(string.Format("Valid request to {0}", application.Context.Request.RawUrl), PxConstants.LOG_CATEGORY);
                PostPageRequestedActivity(pxContext);
            }
            else
            {
                Debug.WriteLine(string.Format("Invalid request to {0}", application.Context.Request.RawUrl), PxConstants.LOG_CATEGORY);
                PostBlockActivity(pxContext);
            }

            // If implemented, run the customVerificationHandler.
            if (!string.IsNullOrEmpty(customVerificationHandler))
            {
                IVerificationHandler customVerificationHandlerInstance = GetCustomVerificationHandler(customVerificationHandler);
                if (customVerificationHandlerInstance != null)
                {
                    customVerificationHandlerInstance.Handle(application, pxContext, config);
                }
                else
                {
                    Debug.WriteLine(string.Format(
                        "Missing implementation of the configured IVerificationHandler ('customVerificationHandler' attribute): {0}.",
                        customVerificationHandler), PxConstants.LOG_CATEGORY);
                }
            }
            else // No custom verification handler -> continue regular flow
            {
                if (!verified && config.MonitorMode == false)
                {
                    BlockRequest(pxContext, config);
                    application.CompleteRequest();
                }
            }
        }

        /// <summary>
        /// Uses reflection to check whether an IVerificationHandler was implemented by the customer. 
        /// </summary>
        /// <returns>If found, returns the IVerificationHandler class instance. Otherwise, returns null.</returns>
        private static IVerificationHandler GetCustomVerificationHandler(string customHandlerName)
        {
            IVerificationHandler customVerificationHandler = null;

            try
            {
                var customVerificationHandlerType =
                    AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(a => a.GetTypes())
                             .FirstOrDefault(t => t.GetInterface(typeof(IVerificationHandler).Name) != null &&
                                                  t.Name.Equals(customHandlerName) && t.IsClass && !t.IsAbstract);

                if (customVerificationHandlerType != null)
                {
                    customVerificationHandler = (IVerificationHandler)Activator.CreateInstance(customVerificationHandlerType, null);
                    Debug.WriteLine(string.Format("Successfully loaded ICustomeVerificationHandler '{0}'.", customHandlerName), PxConstants.LOG_CATEGORY);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                Debug.WriteLine(string.Format("Failed to load the ICustomeVerificationHandler '{0}': {1}.",
                                              customHandlerName, ex.Message), PxConstants.LOG_CATEGORY);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Encountered an error while retrieving the ICustomeVerificationHandler '{0}': {1}.",
                                              customHandlerName, ex.Message), PxConstants.LOG_CATEGORY);
            }

            return customVerificationHandler;
        }
    }
}
