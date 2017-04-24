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

namespace PerimeterX
{

    public class PxModule : IHttpModule
    {
        public static readonly string MODULE_VERSION = "PxModule ASP.NET v1.2.0";
        public const string LOG_CATEGORY = "PxModule";
        private const string CAPTCHA_COOKIE_NAME = "_pxCaptcha";
        private HttpClient httpClient;

        private const string CONFIG_SECTION = "perimeterX/pxModuleConfigurationSection";
        private const string HexAlphabet = "0123456789abcdef";
        private static IActivityReporter reporter;
        private string requestSocketIP;
        private string uuid;
        private string vid;
        private BlockReasonEnum blockReason;
        private string rawRiskCookie;
        private string rawCaptchaCookie;
        private string userAgent = null;
        private const string PX_VALIDATED_HEADER = "X-PX-VALIDATED";
        private const string PX_ORIG_UA_HEADER = "X-PX-ORIG-USER-AGENT";
        private readonly string validationMarker;
        private static readonly Options jsonOptions = new Options(false, true);

        private readonly bool enabled;
        private readonly bool sendPageActivites;
        private readonly bool sendBlockActivities;
        private readonly string cookieName;
        private readonly string userAgentOverride;
        private readonly int blockingScore;
        private readonly string appId;
        private readonly bool suppressContentBlock;
        private readonly bool captchaEnabled;
        private readonly string[] sensetiveHeaders;
        private readonly StringCollection fileExtWhitelist;
        private readonly StringCollection routesWhitelist;
        private readonly StringCollection useragentsWhitelist;
        private readonly string baseUri;
        private readonly string cookieKey;
        private readonly bool signedWithIP;
        private readonly byte[] cookieKeyBytes;
        private readonly bool encryptionEnabled;
        private readonly bool signedWithUserAgent;
        private readonly string socketIpHeader;
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
            cookieName = config.CookieName;
            cookieKey = config.CookieKey;
            cookieKeyBytes = Encoding.UTF8.GetBytes(cookieKey);
            blockingScore = config.BlockingScore;
            appId = config.AppId;
            suppressContentBlock = config.SuppressContentBlock;
            captchaEnabled = config.CaptchaEnabled;
            sensetiveHeaders = config.SensitiveHeaders.Cast<string>().ToArray();
            fileExtWhitelist = config.FileExtWhitelist;
            routesWhitelist = config.RoutesWhitelist;
            userAgentOverride = config.UserAgentOverride;
            useragentsWhitelist = config.UseragentsWhitelist;
            baseUri = string.Format(config.BaseUri,appId);
            signedWithIP = config.SignedWithIP;
            signedWithUserAgent = config.SignedWithUserAgent;
            socketIpHeader = config.SocketIpHeader;
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
                var context = application.Context;
                if (validationMarker == context.Request.Headers[PX_VALIDATED_HEADER])
                {
                    return;
                }
                // Setting custom header for classic mode
                if (HttpRuntime.UsingIntegratedPipeline)
                {
                    context.Request.Headers.Add(PX_VALIDATED_HEADER, validationMarker);
              
                } else
                {
                    var headers = context.Request.Headers;
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
                if (IsValidRequest(context))
                {
                    Debug.WriteLine("Valid request to " + context.Request.RawUrl, LOG_CATEGORY);
                    PostPageRequestedActivity(context);
                    CleanContext(context);

                }
                else
                {
                    Debug.WriteLine("Invalid request to " + context.Request.RawUrl, LOG_CATEGORY);
                    PostBlockActivity(context);
                    BlockRequest(context);
                    CleanContext(context);
                    application.CompleteRequest();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to validate request: " + ex.Message, LOG_CATEGORY);
            }
        }

        private void PostPageRequestedActivity(HttpContext context)
        {
            if (sendPageActivites)
            {
                PostActivity(context, "page_requested");
            }
        }

        private void PostBlockActivity(HttpContext context)
        {
            if (sendBlockActivities)
            {
                PostActivity(context, "block", new ActivityDetails
                {
                    BlockReason = blockReason,
                    BlockUuid = uuid
                });
            }
        }

        private void PostActivity(HttpContext context, string eventType, ActivityDetails details = null)
        {
            var activity = new Activity
            {
                Type = eventType,
                Timestamp = Math.Round(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, MidpointRounding.AwayFromZero),
                AppId = appId,
                Headers = new Dictionary<string, string>(),
                SocketIP = requestSocketIP,
                Url = context.Request.Url.AbsoluteUri,
                Details = details
            };

            for (int i = 0; i < context.Request.Headers.Count; i++)
            {
                var key = context.Request.Headers.GetKey(i);
                if (!IsSensitiveHeader(key))
                {
                    activity.Headers.Add(key, context.Request.Headers.Get(i));
                }
            }

            reporter.Post(activity);
        }

        private void BlockRequest(HttpContext context)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.TrySkipIisCustomErrors = true;
            if (suppressContentBlock)
            {
                context.Response.SuppressContent = true;
            }
            else
            {
                ResponseBlockPage(context);
            }
        }

        private void ResponseBlockPage(HttpContext context)
        {
            var config = (PxModuleConfigurationSection)ConfigurationManager.GetSection(CONFIG_SECTION);
            string template = "block";
            string content;
            if (captchaEnabled)
            {
                template = "captcha";
            }
            Debug.WriteLine(string.Format("Using {0} template", template), LOG_CATEGORY);
            content = TemplateFactory.getTemplate(template, config,uuid,vid);
            context.Response.Write(content);
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

        private bool IsValidRequest(HttpContext context)
        {
            if (!CollectRequestInformation(context))
            {
                return true;
            }

            // check captcha after cookie validation to capture vid
            if (!string.IsNullOrEmpty(rawCaptchaCookie))
            {
                return CheckCaptchaCookie(context);
            }

            // validate using risk cookie
            RiskCookie riskCookie;
            var reason = CheckValidCookie(context, out riskCookie);

            if (reason == RiskRequestReasonEnum.NONE)
            {
                this.vid = riskCookie.Vid;
                this.uuid = riskCookie.Uuid;

                // valid cookie, check if to block or not
                if (IsBlockScores(riskCookie.Scores))
                {
                    this.blockReason = BlockReasonEnum.COOKIE_HIGH_SCORE;
                    Debug.WriteLine(string.Format("Request blocked by risk cookie UUID {0}, VID {1} - {2}", this.uuid, riskCookie.Vid, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
                    return false;
                }
                return true;
            }

            // validate using server risk api
            var risk = CallRiskApi(context, reason);
            if (risk != null && risk.Scores != null && risk.Status == 0 && IsBlockScores(risk.Scores))
            {
                this.uuid = risk.Uuid;
                this.blockReason = BlockReasonEnum.RISK_HIGH_SCORE;
                Debug.WriteLine(string.Format("Request blocked by risk api UUID {0} - {1}", this.uuid, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
                return false;
            }
            return true;
        }

        private bool CheckCaptchaCookie(HttpContext context)
        {
            Debug.WriteLine(string.Format("Check captcha cookie {0} for {1}", this.rawCaptchaCookie, this.vid ?? ""), LOG_CATEGORY);
            try
            {
                var captchaRequest = new CaptchaRequest()
                {
                    Hostname = context.Request.Url.Host,
                    PXCaptcha = this.rawCaptchaCookie,
                    Vid = this.vid,
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
                    Debug.WriteLine(string.Format("Captcha API call to server failed with inner exception {0} - {1}", e.Message, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Captcha API call to server failed with exception {0} - {1}", ex.Message, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
            }
            return false;
        }

        private bool CollectRequestInformation(HttpContext context)
        {
            try
            {
                requestSocketIP = GetSocketIP(context);
                uuid = null;
                vid = null;
                blockReason = BlockReasonEnum.NONE;
                rawCaptchaCookie = null;

                // capture risk cookie
                var pxCookie = context.Request.Cookies.Get(cookieName);
                rawRiskCookie = pxCookie == null ? null : pxCookie.Value;

                // handle captche cookie
                if (captchaEnabled)
                {
                    var captchaCookie = context.Request.Cookies.Get(CAPTCHA_COOKIE_NAME);
                    if (captchaCookie != null && !string.IsNullOrEmpty(captchaCookie.Value))
                    {
                        var captchaCookieParts = captchaCookie.Value.Split(new char[] { ':' }, 2);
                        if (captchaCookieParts.Length == 2)
                        {
                            rawCaptchaCookie = captchaCookieParts[0];
                            vid = captchaCookieParts[1];
                            var expiredCookie = new HttpCookie(CAPTCHA_COOKIE_NAME) { Expires = DateTime.Now.AddDays(-1) };
                            context.Response.Cookies.Add(expiredCookie);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Exception during collecting request information {0} - {1}", ex.Message, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
            }
            return false;
        }

        private bool IsSensitiveHeader(string key)
        {
            return sensetiveHeaders.Contains(key, StringComparer.InvariantCultureIgnoreCase) || key == PX_VALIDATED_HEADER;
        }

        private Request CreateRequestHelper(HttpContext context)
        {
            var headers = new List<RiskRequestHeader>(context.Request.Headers.Count);
            for (int i = 0; i < context.Request.Headers.Count; i++)
            {
                var key = context.Request.Headers.GetKey(i);
                if (!IsSensitiveHeader(key))
                {
                    var header = new RiskRequestHeader
                    {
                        Name = key,
                        Value = context.Request.Headers.Get(i)
                    };
                    headers.Add(header);
                }
            }
            return new Request
            {
                IP = requestSocketIP,
                URL = context.Request.Url.AbsoluteUri,
                Headers = headers.ToArray()
            };
        }

        private RiskResponse CallRiskApi(HttpContext context, RiskRequestReasonEnum reason)
        {
            try
            {
                RiskRequest riskRequest = new RiskRequest
                {
                    Vid = this.vid,
                    Request = CreateRequestHelper(context),
                    Additional = new Additional
                    {
                        HttpMethod = context.Request.HttpMethod,
                        CallReason = reason,
                        HttpVersion = ExtractHttpVersion(context.Request.ServerVariables["SERVER_PROTOCOL"])
                    }
                };

                if (reason == RiskRequestReasonEnum.DECRYPTION_FAILED)
                {
                    riskRequest.Additional.PxOrigCookie = rawRiskCookie;
                }
                return PostRequest<RiskResponse, RiskRequest>(baseUri + "/api/v1/risk", riskRequest);
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Debug.WriteLine(string.Format("Risk API call to server failed with inner exception {0} - {1}", e.Message, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Risk API call to server failed with exception {0} - {1}", ex.Message, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
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

        private RiskRequestReasonEnum CheckValidCookie(HttpContext context, out RiskCookie riskCookie)
        {
            riskCookie = null;
            try
            {
                if (string.IsNullOrEmpty(rawRiskCookie))
                {
                    Debug.WriteLine("Request without risk cookie - " + context.Request.Url.AbsoluteUri, LOG_CATEGORY);
                    return RiskRequestReasonEnum.NO_COOKIE;
                }

                // parse cookie and check if cookie valid
                riskCookie = ParseRiskCookie(rawRiskCookie);
                if (IsRiskCookieExpired(riskCookie))
                {
                    Debug.WriteLine("Request with expired cookie - " + context.Request.Url.AbsoluteUri, LOG_CATEGORY);
                    return RiskRequestReasonEnum.EXPIRED_COOKIE;
                }

                if (string.IsNullOrEmpty(riskCookie.Hash))
                {
                    Debug.WriteLine("Request with invalid cookie (missing signature) - " + context.Request.Url.AbsoluteUri, LOG_CATEGORY);
                    return RiskRequestReasonEnum.INVALID_COOKIE;
                }

                string expectedHash = CalcCookieHash(context, riskCookie);
                if (expectedHash != riskCookie.Hash)
                {
                    Debug.WriteLine(string.Format("Request with invalid cookie (hash mismatch) {0}, expected {1} - {2}", riskCookie.Hash, expectedHash, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
                    return RiskRequestReasonEnum.VALIDATION_FAILED;
                }

                return RiskRequestReasonEnum.NONE;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Request with invalid cookie (exception: " + ex.Message + ") - " + context.Request.Url.AbsoluteUri, LOG_CATEGORY);
            }
            return RiskRequestReasonEnum.DECRYPTION_FAILED;
        }

        private string CalcCookieHash(HttpContext context, RiskCookie riskCookie)
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
            if (signedWithIP && !string.IsNullOrEmpty(this.requestSocketIP))
            {
                sb.Append(this.requestSocketIP);
            }
            // user-agent
            sb.Append(GetSignUserAgent(context));
            string dataToValidate = sb.ToString();

            // calc hmac sha256 as hex string
            var hash = new HMACSHA256(cookieKeyBytes);
            var expectedHashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(dataToValidate));
            return ByteArrayToHexString(expectedHashBytes);
        }

        private string GetSignUserAgent(HttpContext context)
        {
            if (signedWithUserAgent)
            {
                userAgent = context.Request.Headers["user-agent"];
                // if userAgentOverride is present override the default user-agent
                if (userAgentOverride != null && !String.IsNullOrEmpty(context.Request.Headers[userAgentOverride]))
                {
                    // x-px-orig-user-agent should be removed from request as well as user-agent should return old value,
                    // CleanContext should be called at the end of the module
                    context.Request.Headers[PX_ORIG_UA_HEADER] = userAgent;

                    userAgent = context.Request.Headers[userAgentOverride];
                    context.Request.Headers["user-agent"] = userAgent;

                }

                if (userAgent != null)
                {
                    return userAgent;
                }
            }
            return string.Empty;
        }


        /**
         * Removes all PX enrichment from the context, until pxCtx will be used to hold
        */
        private void CleanContext(HttpContext context)
        {
            if (userAgentOverride != null && !string.IsNullOrEmpty(context.Request.Headers.Get(userAgentOverride)))
            {
                string pxOrigUserAgent = context.Request.Headers.Get(PX_ORIG_UA_HEADER);
                context.Request.Headers.Remove(PX_ORIG_UA_HEADER);
                context.Request.Headers["user-agent"] = pxOrigUserAgent;
            }
        }

        private string GetSocketIP(HttpContext context)
        {
            try
            {
                var ip = context.Request.UserHostAddress;
                if (!string.IsNullOrEmpty(socketIpHeader))
                {
                    var headerVal = context.Request.Headers[socketIpHeader];
                    if (headerVal != null)
                    {
                        var ips = headerVal.Split(new char[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        IPAddress firstIpAddress;
                        if (ips.Length > 0 && IPAddress.TryParse(ips[0], out firstIpAddress))
                        {
                            return ips[0];
                        }
                    }
                }
                return ip;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Failed to extract request socket IP {0} - {1}", ex.Message, context.Request.Url.AbsoluteUri), LOG_CATEGORY);
            }
            return string.Empty;
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
