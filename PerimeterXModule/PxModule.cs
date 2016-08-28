using System;
using System.Web;
using System.Security.Cryptography;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Linq;
using System.Diagnostics;
using System.Collections.Specialized;

namespace PerimeterX
{

    public class PxModule : IHttpModule
    {
        public const string MODULE_VERSION = "PxModule ASP.NET v1.0";
        private const int KEY_SIZE_BITS = 256;
        private const int IV_SIZE_BITS = 128;
        private const string CONFIG_SECTION = "perimeterX/pxModuleConfigurationSection";
        private const string HexAlphabet = "0123456789abcdef";
        private readonly PxModuleReporter reporter;
        private string requestSocketIP;
        private string uuid;
        private string blockReason;
        private string rawRiskCookie;

        public PxModule()
        {
            if (Config == null)
            {
                throw new ConfigurationErrorsException("Missing PerimeterX module configuration section");
            }

            reporter = PxModuleReporter.Instance(Config);
            Debug.WriteLine("PxModule initialized");
        }

        public string ModuleName
        {
            get { return "PxModule"; }
        }

        public void Init(HttpApplication application)
        {
            application.BeginRequest += new EventHandler(this.Application_BeginRequest);
        }

        private void Application_BeginRequest(object source, EventArgs e)
        {
            HttpApplication application = (HttpApplication)source;
            if (application == null || IsFilteredRequest(application.Context))
            {
                return;
            }
            HttpContext context = application.Context;
            if (IsValidRequest(context))
            {
                PostPageRequestedActivity(context);
            }
            else
            {
                PostBlockActivity(context);
                BlockRequest(context);
            }
        }

        private void PostPageRequestedActivity(HttpContext context)
        {
            if (Config.SendPageActivites)
            {
                PostActivity(context, "page_requested");
            }
        }

        private void PostBlockActivity(HttpContext context)
        {
            if (Config.SendBlockActivites)
            {
                var details = new Dictionary<string, object>();
                if (blockReason != null)
                {
                    details.Add("block_reason", blockReason);
                }
                if (uuid != null)
                {
                    details.Add("block_uuid", uuid);
                }
                PostActivity(context, "block", details);
            }
        }

        private void PostActivity(HttpContext context, string eventType, Dictionary<string, object> details = null)
        {
            var activity = new Activity
            {
                Type = eventType,
                Timestamp = Math.Round(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, MidpointRounding.AwayFromZero),
                AppId = Config.AppId,
                Headers = new Dictionary<string, object>(),
                SocketIP = requestSocketIP,
                Url = context.Request.RawUrl,
                Details = details
            };
            foreach (var kv in GetNonSensitiveHeaders(context.Request.Headers))
            {
                activity.Headers.Add(kv.Key, kv.Value);
            }
            reporter.Post(activity);
        }

        private void BlockRequest(HttpContext context)
        {
            var config = Config;
            context.Response.StatusCode = config.BlockStatusCode;
            if (config.InternalBlockPage)
            {
                ResponseInternalBlockPage(context);
            }
            else
            {
                context.Response.SuppressContent = true;
                context.Response.End();
            }
        }

        private void ResponseInternalBlockPage(HttpContext context)
        {
            var id = uuid ?? string.Empty;
            string content = @"<html lang=""en""><head> <link type=""text / css"" rel=""stylesheet"" media=""screen, print"" href=""//fonts.googleapis.com/css?family=Open+Sans:300italic,400italic,600italic,700italic,800italic,400,300,600,700,800""> <meta charset=""UTF-8""> <title>Title</title> <style> p { width: 60%; margin: 0 auto; font-size: 35px; } body { background-color: #a2a2a2; font-family: ""Open Sans""; margin: 5%; } img { widht: 180px; } a { color: #2020B1; text-decoration: blink; } a:hover { color: #2b60c6; } </style> <style type=""text/css""></style></head><body cz-shortcut-listen=""true""><div><img src=""http://storage.googleapis.com/instapage-thumbnails/035ca0ab/e94de863/1460594818-1523851-467x110-perimeterx.png""></div><span style=""color: white; font-size: 34px;"">Access to This Page Has Been Blocked</span><div style=""font-size: 24px;color: #000042;""><br> Access to '" +
                context.Request.RawUrl +
                @"' is blocked according to the site security policy. <br> Your browsing behaviour fingerprinting made us think you may be a bot. <br> <br> This may happen as a result of the following: <ul> <li>JavaScript is disabled or not running properly.</li> <li>Your browsing behaviour fingerprinting are not likely to be a regular user.</li> </ul> To read more about the bot defender solution: <a href=""https://www.perimeterx.com/bot-defender"">https://www.perimeterx.com/bot-defender</a> <br> If you think the blocking was done by mistake, contact the site administrator. <br> <br> </br>" +
                id +
                @"</div></body></html>";
            context.Response.Write(content);
        }

        //public void Dispose()
        //{
            //if (httpClient != null)
            //{
                //httpClient.Dispose();
                //httpClient = null;
            //}
        //}

        private bool IsFilteredRequest(HttpContext context)
        {
            if (!Config.Enabled)
            {
                return true;
            }
            // TODO(barak): go over all supported filters types
            //context.Request.HttpMethod.ToUpper() != "GET"
            string ignoreUrlRegex = Config.IgnoreUrlRegex;
            if (!string.IsNullOrEmpty(ignoreUrlRegex) && Regex.IsMatch(context.Request.RawUrl, ignoreUrlRegex))
            {
                Debug.WriteLine("Skip, ignore URL regex matched for " + context.Request.RawUrl);
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

            // validae using risk cookie
            RiskCookie riskCookie;
            var reason = CheckValidCookie(context, out riskCookie);
            if (reason == RiskRequestReasonEnum.NONE)
            {
                this.uuid = riskCookie.Uuid;
                // valid cookie, check if to block or not
                if (IsBlockScores(riskCookie.Scores))
                {
                    this.blockReason = "cookie_high_score";
                    Debug.WriteLine(string.Format("Request blocked by risk cookie UUID {0}, VID {1} - {2}", this.uuid, riskCookie.Vid, context.Request.RawUrl));
                    return false;
                }
                return true;
            }

            // validate using server risk api
            var risk = FetchGetRisk(context, reason);
            if (risk == null)
            {
                // failed to get response from server
                return true;
            }
            if (risk.Scores != null && IsBlockScores(risk.Scores))
            {
                this.uuid = risk.Uuid;
                this.blockReason = "risk_high_score";
                Debug.WriteLine(string.Format("Request blocked by risk api UUID {0} - {1}", this.uuid, context.Request.RawUrl));
                return false;
            }
            return true;
        }

        private bool CollectRequestInformation(HttpContext context)
        {
            try
            {
                requestSocketIP = GetSocketIP(context);
                uuid = null;
                blockReason = null;
                HttpCookie pxCookie = context.Request.Cookies.Get(Config.CookieName);
                rawRiskCookie = pxCookie == null ? null : pxCookie.Value;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Exception during collecting request information {0} - {1}", ex.Message, context.Request.RawUrl));
            }
            return false;
        }
        private IEnumerable<KeyValuePair<string, string>> GetNonSensitiveHeaders(NameValueCollection headers)
        {
            var sensitiveHeaders = Config.SensitiveHeaders;
            for (int i = 0; i < headers.Count; i++)
            {
                var key = headers.GetKey(i);
                if (!IsSensitiveHeader(sensitiveHeaders, key))
                {
                    var kv = new KeyValuePair<string, string>(key, headers.Get(i));
                    yield return kv;
                }
            }
        }

        private static bool IsSensitiveHeader(StringCollection sensitiveHeaders, string key)
        {
            foreach (var header in sensitiveHeaders)
            {
                if (string.Compare(header, key, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return true;
                }

            }
            return false;
        }

        private RiskResponse FetchGetRisk(HttpContext context, RiskRequestReasonEnum reason)
        {
            try
            {
                RiskRequest riskRequest = new RiskRequest
                {
                    Request = new RiskRequestRequest
                    {
                        IP = requestSocketIP,
                        URI = context.Request.RawUrl,
                        URL = context.Request.RawUrl,
                        Headers = GetNonSensitiveHeaders(context.Request.Headers)
                            .Select(kv => new RiskRequestHeader()
                            {
                                Name = kv.Key,
                                Value = kv.Value
                            })
                            .ToArray()
                    },
                    Additional = new RiskRequestAdditional
                    {
                        HttpMethod = context.Request.HttpMethod,
                        CallReason = reason,
                        PXCookie = rawRiskCookie,
                        HttpVersion = "1.1" // TODO(barak): extract from request
                    }
                };
                var riskResponse = PostRiskRequest(riskRequest);
                //var riskRequestJson = PxModuleJson.StringifyObject(riskRequest);
                //string riskUri = Config.BaseUri + @"/api/v1/risk";
                //var requestMessage = new HttpRequestMessage(HttpMethod.Post, riskUri);
                //requestMessage.Content = new StringContent(riskRequestJson, Encoding.UTF8, "application/json");
                //var response = httpClient.SendAsync(requestMessage).Result;
                //response.EnsureSuccessStatusCode();
                //var contentJson = response.Content.ReadAsStringAsync().Result;
                //Debug.WriteLine(string.Format("Risk API call for {0}, returned {1}", context.Request.RawUrl, contentJson));
                //var riskResponse = PxModuleJson.ParseObject<RiskResponse>(contentJson);
                return riskResponse;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Risk API call to server failed with exception {0} - {1}", ex.Message, context.Request.RawUrl));
            }
            return null;
        }

        private RiskResponse PostRiskRequest(RiskRequest riskRequest)
        {
            var config = Config;
            var riskRequestJson = PxModuleJson.StringifyObject(riskRequest);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(config.BaseUri + "/api/v1/risk");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("Authorization", "Bearer " + config.ApiToken);
            httpWebRequest.Timeout = config.ApiTimeout;
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(riskRequestJson);
            }
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var jsonResponse = streamReader.ReadToEnd();
                    Debug.WriteLine(string.Format("Risk call {0} for {1}, returned {2}",
                                riskRequestJson, context.Request.RawUrl, jsonResponse));
                    var riskResponse = PxModuleJson.ParseObject<RiskResponse>(jsonResponse);
                    return riskResponse;
                }
            }
            return null;
        }

        private bool IsBlockScores(RiskScores scores)
        {
            var blockScore = Config.BlockingScore;
            return scores != null && (scores.Bot >= blockScore || scores.Application >= blockScore);
        }

        private static string DecodeCookie(string cookie)
        {
            byte[] bytes = Convert.FromBase64String(cookie);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string DecryptCookie(string cookieKey, string cookie)
        {
            if (cookieKey == null)
            {
                throw new ArgumentNullException("cookieKey");
            }
            if (cookie == null)
            {
                throw new ArgumentNullException("cookie");
            }
            string[] parts = cookie.Split(new char[] { ':' }, 3);
            if (parts.Length != 3)
            {
                throw new InvalidDataException("PX cookie format");
            }
            byte[] salt = Convert.FromBase64String(parts[0]);
            int iterations = int.Parse(parts[1]);
            if (iterations < 1 || iterations > 1000)
            {
                throw new ArgumentOutOfRangeException("iterations", "encryption iterations");
            }
            byte[] data = Convert.FromBase64String(parts[2]);

            byte[] cookieKeyBytes = Encoding.UTF8.GetBytes(cookieKey);
            using (MemoryStream ms = new MemoryStream())
            using (RijndaelManaged AES = new RijndaelManaged())
            {
                var key = new byte[KEY_SIZE_BITS / 8];
                var iv = new byte[IV_SIZE_BITS / 8];
                var dk = PBKDF2Sha256GetBytes(key.Length + iv.Length, cookieKeyBytes, salt, iterations);
                Array.Copy(dk, key, key.Length);
                Array.Copy(dk, key.Length, iv, 0, iv.Length);

                AES.KeySize = KEY_SIZE_BITS;
                AES.BlockSize = IV_SIZE_BITS;
                AES.Key = key;
                AES.IV = iv;
                AES.Mode = CipherMode.CBC;
                using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Close();
                }
                var decryptedBytes = ms.ToArray();
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }


        public static RiskCookie ParseRiskCookie(string cookieData, bool encrypted, string cookieKey)
        {
            string cookieJson;
            if (encrypted)
            {
                cookieJson = DecryptCookie(cookieKey, cookieData);
            }
            else
            {
                cookieJson = DecodeCookie(cookieData);
            }
            var riskCookie = PxModuleJson.ParseObject<RiskCookie>(cookieJson);
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
                var config = Config;

                // get cookie
                if (string.IsNullOrEmpty(rawRiskCookie))
                {
                    Debug.WriteLine("Request without risk cookie - " + context.Request.RawUrl);
                    return RiskRequestReasonEnum.NO_COOKIE;
                }
                // parse cookie
                riskCookie = ParseRiskCookie(rawRiskCookie, config.EncryptionEnabled, config.CookieKey);
                // check if expired
                if (IsRiskCookieExpired(riskCookie))
                {
                    Debug.WriteLine("Request with expired cookie - " + context.Request.RawUrl);
                    return RiskRequestReasonEnum.EXPIRED_COOKIE;
                }

                if (string.IsNullOrEmpty(riskCookie.Hash))
                {
                    Debug.WriteLine("Request with invalid cookie (missing signature) - " + context.Request.RawUrl);
                    return RiskRequestReasonEnum.INVALID_COOKIE;
                }

                string expectedHash = CalcCookieHash(context, riskCookie);
                if (expectedHash != riskCookie.Hash)
                {
                    Debug.WriteLine("Request with invalid cookie (hash mismatch) {0}, expected {1} - {2}", riskCookie.Hash, expectedHash, context.Request.RawUrl);
                    return RiskRequestReasonEnum.INVALID_COOKIE;
                }

                return RiskRequestReasonEnum.NONE;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Request with invalid cookie (exception: " + ex.Message + ") - " + context.Request.RawUrl);
            }
            return RiskRequestReasonEnum.INVALID_COOKIE;
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
            if (Config.SignedWithIP && !string.IsNullOrEmpty(this.requestSocketIP))
            {
                sb.Append(this.requestSocketIP);
            }
            // user-agent
            sb.Append(GetSignUserAgent(context));
            string dataToValidate = sb.ToString();

            // calc hmac sha256 as hex string
            var cookieKeyBytes = Encoding.UTF8.GetBytes(Config.CookieKey);
            var hash = new HMACSHA256(cookieKeyBytes);
            var expectedHashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(dataToValidate));
            return ByteArrayToHexString(expectedHashBytes);
        }

        private string GetSignUserAgent(HttpContext context)
        {
            if (Config.SignedWithUserAgent)
            {
                var userAgent = context.Request.Headers["user-agent"];
                return userAgent ?? string.Empty;
            }
            return "";
        }

        private string GetSocketIP(HttpContext context)
        {
            try
            {
                var socketIpHeader = Config.SocketIpHeader;
                if (string.IsNullOrEmpty(socketIpHeader))
                {
                    return context.Request.UserHostAddress;
                }
                var ip = context.Request.Headers[socketIpHeader];
                return (ip != null) ? ip.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Failed to extract request socket IP {0} - {1}", ex.Message, context.Request.RawUrl));
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

        private static byte[] PBKDF2Sha256GetBytes(int dklen, byte[] password, byte[] salt, int iterationCount)
        {
            using (var hmac = new HMACSHA256(password))
            {
                int hashLength = hmac.HashSize / 8;
                if ((hmac.HashSize & 7) != 0)
                {
                    hashLength++;
                }
                int keyLength = dklen / hashLength;
                if (dklen > (0xFFFFFFFFL * hashLength) || dklen < 0)
                {
                    throw new ArgumentOutOfRangeException("dklen");
                }
                if (dklen % hashLength != 0)
                {
                    keyLength++;
                }
                byte[] extendedkey = new byte[salt.Length + 4];
                Buffer.BlockCopy(salt, 0, extendedkey, 0, salt.Length);
                using (var ms = new System.IO.MemoryStream())
                {
                    for (int i = 0; i < keyLength; i++)
                    {
                        extendedkey[salt.Length] = (byte)(((i + 1) >> 24) & 0xFF);
                        extendedkey[salt.Length + 1] = (byte)(((i + 1) >> 16) & 0xFF);
                        extendedkey[salt.Length + 2] = (byte)(((i + 1) >> 8) & 0xFF);
                        extendedkey[salt.Length + 3] = (byte)(((i + 1)) & 0xFF);
                        byte[] u = hmac.ComputeHash(extendedkey);
                        Array.Clear(extendedkey, salt.Length, 4);
                        byte[] f = u;
                        for (int j = 1; j < iterationCount; j++)
                        {
                            u = hmac.ComputeHash(u);
                            for (int k = 0; k < f.Length; k++)
                            {
                                f[k] ^= u[k];
                            }
                        }
                        ms.Write(f, 0, f.Length);
                        Array.Clear(u, 0, u.Length);
                        Array.Clear(f, 0, f.Length);
                    }
                    byte[] dk = new byte[dklen];
                    ms.Position = 0;
                    ms.Read(dk, 0, dklen);
                    ms.Position = 0;
                    for (long i = 0; i < ms.Length; i++)
                    {
                        ms.WriteByte(0);
                    }
                    Array.Clear(extendedkey, 0, extendedkey.Length);
                    return dk;
                }
            }
        }
        private PxModuleConfigurationSection Config
        {
            get
            {
                return (PxModuleConfigurationSection)ConfigurationManager.GetSection(CONFIG_SECTION);
            }
        }
    }

}

