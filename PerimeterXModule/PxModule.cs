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
using Jil;
using System.Net;
using System.Linq;
using System.Reflection;
using System.Collections;
using PerimeterX.Internals;

namespace PerimeterX
{

    public class PxModule : IHttpModule
    {
        public static readonly string MODULE_VERSION = "PxModule ASP.NET v1.0";
        public const string LOG_CATEGORY = "PxModule";
        private const string CAPTCHA_COOKIE_NAME = "_pxCaptcha";
        private HttpClient httpClient;
        private PxContext context;

        private const string CONFIG_SECTION = "perimeterX/pxModuleConfigurationSection";
        private const string HexAlphabet = "0123456789abcdef";
        private static IActivityReporter reporter;
        private const string PX_VALIDATED_HEADER = "X-PX-VALIDATED";
        private readonly string validationMarker;
        private static readonly Options jsonOptions = new Options(false, true);
        private readonly bool enabled;
        private readonly bool sendPageActivites;
        private readonly bool sendBlockActivities;
        private readonly bool encryptionEnabled;
        private readonly int blockingScore;
        private readonly string appId;
        private readonly bool suppressContentBlock;
        private readonly bool captchaEnabled;
        private readonly string[] sensetiveHeaders;
        private readonly StringCollection fileExtWhitelist;
        private readonly StringCollection routesWhitelist;
        private readonly StringCollection useragentsWhitelist;
        private readonly string baseUri;
        private readonly bool signedWithIP;
        private readonly string cookieKey;
        private readonly byte[] cookieKeyBytes;
        private readonly ICookieDecoder cookieDecoder;

        static PxModule()
        {
            var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(CONFIG_SECTION);
            // allocate reporter if needed
            if (config != null && (config.SendBlockActivites || config.SendPageActivites))
            {
                reporter = new ActivityReporter(config.BaseUri, config.ActivitiesCapacity, config.ActivitiesBulkSize, config.ReporterApiTimeout);
            }
            else
            {
                reporter = new NullActivityMonitor();
            }
            try
            {
                MODULE_VERSION = "PxModule ASP.NET v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to extract assembly version " + ex.Message, LOG_CATEGORY);
            }
        }

        public PxModule()
        {
            var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(CONFIG_SECTION);
            if (config == null)
            {
                throw new ConfigurationErrorsException("Missing PerimeterX module configuration section " + CONFIG_SECTION);
            }

            // load configuration
            enabled = config.Enabled;
            encryptionEnabled = config.EncryptionEnabled;
            sendPageActivites = config.SendPageActivites;
            sendBlockActivities = config.SendBlockActivites;
            cookieKey = config.CookieKey;
            cookieKeyBytes = Encoding.UTF8.GetBytes(cookieKey);
            blockingScore = config.BlockingScore;
            appId = config.AppId;
            suppressContentBlock = config.SuppressContentBlock;
            captchaEnabled = config.CaptchaEnabled;
            sensetiveHeaders = config.SensitiveHeaders.Cast<string>().ToArray();
            fileExtWhitelist = config.FileExtWhitelist;
            routesWhitelist = config.RoutesWhitelist;
            useragentsWhitelist = config.UseragentsWhitelist;
            baseUri = string.Format(config.BaseUri,appId);
            signedWithIP = config.SignedWithIP;
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
            Debug.WriteLine(ModuleName + " initialized", LOG_CATEGORY);
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
                if (validationMarker == applicationContext.Request.Headers[PX_VALIDATED_HEADER])
                {
                    return;
                }
                // Setting custom header for classic mode
                if (HttpRuntime.UsingIntegratedPipeline)
                {
                    applicationContext.Request.Headers.Add(PX_VALIDATED_HEADER, validationMarker);
              
                } else
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
                        new object[] { PX_VALIDATED_HEADER, new ArrayList { validationMarker } });
                    // repeat BaseAdd invocation for any other headers to be added
                    // Then set the collection back to ReadOnly
                    ro.SetValue(headers, true, null);
                }
                if (IsValidRequest(applicationContext))
                {
                    Debug.WriteLine("Valid request to " + applicationContext.Request.RawUrl, LOG_CATEGORY);
                    PostPageRequestedActivity(context);

                }
                else
                {
                    Debug.WriteLine("Invalid request to " + applicationContext.Request.RawUrl, LOG_CATEGORY);
                    PostBlockActivity(context);
                    BlockRequest(context);
                    application.CompleteRequest();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to validate request: " + ex.Message, LOG_CATEGORY);
            }
        }

        private void PostPageRequestedActivity(PxContext context)
        {
            if (sendPageActivites)
            {
                PostActivity(context, "page_requested");
            }
        }

        private void PostBlockActivity(PxContext context)
        {
            if (sendBlockActivities)
            {
                PostActivity(context, "block", new ActivityDetails
                {
                    BlockReason = context.BlockReason,
                    BlockUuid = context.UUID
                });
            }
        }

        private void PostActivity(PxContext context, string eventType, ActivityDetails details = null)
        {
            var activity = new Activity
            {
                Type = eventType,
                Timestamp = Math.Round(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, MidpointRounding.AwayFromZero),
                AppId = appId,
                Headers = new Dictionary<string, string>(),
                SocketIP = context.Ip,
                Url = context.FullUrl,
                Details = details
            };

            foreach ( RiskRequestHeader riskHeader in context.Headers)
            {
                var key = riskHeader.Name;
                if (!IsSensitiveHeader(key))
                {
                    activity.Headers.Add(key, riskHeader.Value);
                }
            }
               
            reporter.Post(activity);
        }

        private void BlockRequest(PxContext context)
        {
            context.ApplicationContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.ApplicationContext.Response.TrySkipIisCustomErrors = true;
            if (suppressContentBlock)
            {
                context.ApplicationContext.Response.SuppressContent = true;
            }
            else
            {
                ResponseBlockPage(context);
            }
        }

        private void ResponseBlockPage(PxContext context)
        {
            var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(CONFIG_SECTION);
            string template = "block";
            string content;
            if (captchaEnabled)
            {
                template = "captcha";
            }
            Debug.WriteLine(string.Format("Using {0} template", template), LOG_CATEGORY);
            content = TemplateFactory.getTemplate(template, config, context.UUID, context.Vid);
            context.ApplicationContext.Response.Write(content);
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

        private bool IsValidRequest(HttpContext applicationContext)
        {
            context = new PxContext(applicationContext, (PxModuleConfigurationSection)ConfigurationManager.GetSection(CONFIG_SECTION));

            // check captcha after cookie validation to capture vid
            if (!string.IsNullOrEmpty(context.PxCaptcha))
            {
                return CheckCaptchaCookie(context);
            }

            // validate using risk cookie
            RiskCookie riskCookie;
            var reason = CheckValidCookie(context, out riskCookie);

            if (reason == RiskRequestReasonEnum.NONE)
            {
                context.Vid = riskCookie.Vid;
                context.UUID = riskCookie.Uuid;

                // valid cookie, check if to block or not
                if (IsBlockScores(riskCookie.Scores))
                {
                    context.BlockReason = BlockReasonEnum.COOKIE_HIGH_SCORE;
                    Debug.WriteLine(string.Format("Request blocked by risk cookie UUID {0}, VID {1} - {2}", context.UUID, riskCookie.Vid, context.Uri), LOG_CATEGORY);
                    return false;
                }
                return true;
            }

            // validate using server risk api
            var risk = CallRiskApi(context, reason);
            if (risk != null && risk.Scores != null && risk.Status == 0 && IsBlockScores(risk.Scores))
            {
                context.UUID = risk.Uuid;
                context.BlockReason = BlockReasonEnum.RISK_HIGH_SCORE;
                Debug.WriteLine(string.Format("Request blocked by risk api UUID {0} - {1}", context.UUID, context.Uri), LOG_CATEGORY);
                return false;
            }
            return true;
        }

        private bool CheckCaptchaCookie(PxContext context)
        {
            Debug.WriteLine(string.Format("Check captcha cookie {0} for {1}", context.PxCaptcha, context.Vid ?? ""), LOG_CATEGORY);
            try
            {
                var captchaRequest = new CaptchaRequest()
                {
                    Hostname = context.Hostname,
                    PXCaptcha = context.PxCaptcha,
                    Vid = context.Vid,
                    Request = CreateRequestHelper(context)
                };
                var response = PostRequest<CaptchaResponse, CaptchaRequest>(baseUri + "/api/v1/risk/captcha", captchaRequest);
                if (response != null && response.Status == 0)
                {
                    Debug.WriteLine("Captcha API call to server was successful", LOG_CATEGORY);
                    return true;
                }
                Debug.WriteLine(string.Format("Captcha API call to server failed - {0}", response), LOG_CATEGORY);
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Debug.WriteLine(string.Format("Captcha API call to server failed with inner exception {0} - {1}", e.Message, context.Uri), LOG_CATEGORY);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Captcha API call to server failed with exception {0} - {1}", ex.Message, context.Uri), LOG_CATEGORY);
            }
            return false;
        }

        private bool IsSensitiveHeader(string key)
        {
            return sensetiveHeaders.Contains(key, StringComparer.InvariantCultureIgnoreCase) || key == PX_VALIDATED_HEADER;
        }

        private Request CreateRequestHelper(PxContext context)
        {
            var headers = new List<RiskRequestHeader>(context.Headers.Count);
            foreach (RiskRequestHeader header in context.Headers)
            {
                var key = header.Name;
                if (!IsSensitiveHeader(key))
                {
                    headers.Add(header);
                }
            }
            return new Request
            {
                IP = context.Ip,
                URL = context.Uri,
                Headers = headers.ToArray()
            };
        }

        private RiskResponse CallRiskApi(PxContext context, RiskRequestReasonEnum reason)
        {
            try
            {
                RiskRequest riskRequest = new RiskRequest
                {
                    Vid = context.Vid,
                    Request = CreateRequestHelper(context),
                    Additional = new Additional
                    {
                        HttpMethod = context.HttpMethod,
                        CallReason = reason,
                        HttpVersion = context.HttpVersion
                    }
                };

                if (reason == RiskRequestReasonEnum.DECRYPTION_FAILED)
                {
                    riskRequest.Additional.PxOrigCookie = context.getPxCookie();
                }
                return PostRequest<RiskResponse, RiskRequest>(baseUri + "/api/v1/risk", riskRequest);
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Debug.WriteLine(string.Format("Risk API call to server failed with inner exception {0} - {1}", e.Message, context.Uri), LOG_CATEGORY);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Risk API call to server failed with exception {0} - {1}", ex.Message, context.Uri), LOG_CATEGORY);
            }
            return null;
        }

        private static string ExtractHttpVersion(string serverProtocol)
        {
            if (serverProtocol != null)
            {
                int i = serverProtocol.IndexOf("/");
                if (i != -1)
                {
                    return serverProtocol.Substring(i + 1);
                }
            }
            return serverProtocol;
        }

        private R PostRequest<R, T>(string url, T request)
        {
            var requestJson = JSON.Serialize<T>(request, jsonOptions);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            var httpResponse = this.httpClient.SendAsync(requestMessage).Result;
            httpResponse.EnsureSuccessStatusCode();
            var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
            Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", url, requestJson, responseJson), LOG_CATEGORY);
            return JSON.Deserialize<R>(responseJson, jsonOptions);
        }

        private bool IsBlockScores(RiskResponseScores scores)
        {
            return scores != null && (scores.Filter >= blockingScore || scores.Bot >= blockingScore || scores.Application >= blockingScore);
        }

        private bool IsBlockScores(RiskCookieScores scores)
        {
            return scores != null && (scores.Bot >= blockingScore || scores.Application >= blockingScore);
        }


        private static string DecodeCookie(string cookie)
        {
            byte[] bytes = Convert.FromBase64String(cookie);
            return Encoding.UTF8.GetString(bytes);
        }

        public RiskCookie ParseRiskCookie(string cookieData)
        {
            string cookieJson = cookieDecoder.Decode(cookieData);
            Debug.WriteLineIf(cookieJson != null, "Parse risk cookie " + cookieJson, LOG_CATEGORY);
            var riskCookie = JSON.Deserialize<RiskCookie>(cookieJson, jsonOptions);
            return riskCookie;
        }

        public static bool IsRiskCookieExpired(RiskCookie riskCookie)
        {
            if (riskCookie == null)
            {
                throw new ArgumentNullException("riskCookie");
            }
            double now = DateTime.UtcNow
                    .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                    .TotalMilliseconds;
            return (riskCookie.Time < now);
        }

        private RiskRequestReasonEnum CheckValidCookie(PxContext context, out RiskCookie riskCookie)
        {
            riskCookie = null;
            try
            {
                if (string.IsNullOrEmpty(context.getPxCookie()))
                {
                    Debug.WriteLine("Request without risk cookie - " + context.Uri, LOG_CATEGORY);
                    return RiskRequestReasonEnum.NO_COOKIE;
                }

                // parse cookie and check if cookie valid
                riskCookie = ParseRiskCookie(context.getPxCookie());
                if (IsRiskCookieExpired(riskCookie))
                {
                    Debug.WriteLine("Request with expired cookie - " + context.Uri, LOG_CATEGORY);
                    return RiskRequestReasonEnum.EXPIRED_COOKIE;
                }

                if (string.IsNullOrEmpty(riskCookie.Hash))
                {
                    Debug.WriteLine("Request with invalid cookie (missing signature) - " + context.Uri, LOG_CATEGORY);
                    return RiskRequestReasonEnum.INVALID_COOKIE;
                }

                string expectedHash = CalcCookieHash(context, riskCookie);
                if (expectedHash != riskCookie.Hash)
                {
                    Debug.WriteLine(string.Format("Request with invalid cookie (hash mismatch) {0}, expected {1} - {2}", riskCookie.Hash, expectedHash, context.Uri), LOG_CATEGORY);
                    return RiskRequestReasonEnum.VALIDATION_FAILED;
                }

                return RiskRequestReasonEnum.NONE;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Request with invalid cookie (exception: " + ex.Message + ") - " + context.Uri, LOG_CATEGORY);
            }
            return RiskRequestReasonEnum.DECRYPTION_FAILED;
        }

        private string CalcCookieHash(PxContext context, RiskCookie riskCookie)
        {
            // build string with data to validate
            var sb = new StringBuilder();
            // timestamp
            sb.Append(riskCookie.Time);
            // scores
            if (riskCookie.Scores != null)
            {
                sb.Append(riskCookie.Scores.Application);
                sb.Append(riskCookie.Scores.Bot);
            }
            // uuid
            if (!string.IsNullOrEmpty(riskCookie.Uuid))
            {
                sb.Append(riskCookie.Uuid);
            }
            // vid
            if (!string.IsNullOrEmpty(riskCookie.Vid))
            {
                sb.Append(riskCookie.Vid);
            }
            // socket ip
            if (signedWithIP && !string.IsNullOrEmpty(context.Ip))
            {
                sb.Append(context.Ip);
            }
            // user-agent
            sb.Append(context.UserAgent);
            string dataToValidate = sb.ToString();

            // calc hmac sha256 as hex string
            var hash = new HMACSHA256(cookieKeyBytes);
            var expectedHashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(dataToValidate));
            return ByteArrayToHexString(expectedHashBytes);
        }


     

        private static string ByteArrayToHexString(byte[] input)
        {
            StringBuilder sb = new StringBuilder(input.Length * 2);
            foreach (byte b in input)
            {
                sb.Append(HexAlphabet[b >> 4]);
                sb.Append(HexAlphabet[b & 0xF]);
            }
            return sb.ToString();
        }

    }
}
