using System;
using System.Web;
using System.Security.Cryptography;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Linq;
using System.Collections.Specialized;

namespace PerimeterX
{

    public class PxModule : IHttpModule
    {
        private HttpClient httpClient;
        private const int KEY_SIZE_BITS = 256;
        private const int IV_SIZE_BITS = 128;
        private static readonly string CONFIG_SECTION = "perimeterX/pxModuleConfigurationSection";
        public static string UUID_ITEM_KEY = "PXUUID";
        private const string HexAlphabet = "0123456789abcdef";


        public PxModule()
        {
            if (Config == null)
            {
                throw new ConfigurationErrorsException("Missing PerimeterX module configuration section");
            }

            httpClient = new HttpClient()
            {
                Timeout = Config.BackchannelTimeout,
                MaxResponseContentBufferSize = 1024 * 1024 * 10
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Config.ApiToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PerimeterX middleware");
            httpClient.DefaultRequestHeaders.ExpectContinue = false;
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
            if (application != null)
            {
                HttpContext context = application.Context;
                if (!IsValidRequest(context))
                {
                    BlockRequest(context);
                }
            }
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
            }
            context.Response.End();
        }

        private void ResponseInternalBlockPage(HttpContext context)
        {
            string id = (string)context.Items[UUID_ITEM_KEY];
            if (id == null)
            {
                id = string.Empty;
            }
            string content = @"<html lang=""en""><head> <link type=""text / css"" rel=""stylesheet"" media=""screen, print"" href=""//fonts.googleapis.com/css?family=Open+Sans:300italic,400italic,600italic,700italic,800italic,400,300,600,700,800""> <meta charset=""UTF-8""> <title>Title</title> <style> p { width: 60%; margin: 0 auto; font-size: 35px; } body { background-color: #a2a2a2; font-family: ""Open Sans""; margin: 5%; } img { widht: 180px; } a { color: #2020B1; text-decoration: blink; } a:hover { color: #2b60c6; } </style> <style type=""text/css""></style></head><body cz-shortcut-listen=""true""><div><img src=""http://storage.googleapis.com/instapage-thumbnails/035ca0ab/e94de863/1460594818-1523851-467x110-perimeterx.png""></div><span style=""color: white; font-size: 34px;"">Access to This Page Has Been Blocked</span><div style=""font-size: 24px;color: #000042;""><br> Access to '" +
                context.Request.RawUrl + 
                @"' is blocked according to the site security policy. <br> Your browsing behaviour fingerprinting made us think you may be a bot. <br> <br> This may happen as a result of the following: <ul> <li>JavaScript is disabled or not running properly.</li> <li>Your browsing behaviour fingerprinting are not likely to be a regular user.</li> </ul> To read more about the bot defender solution: <a href=""https://www.perimeterx.com/bot-defender"">https://www.perimeterx.com/bot-defender</a> <br> If you think the blocking was done by mistake, contact the site administrator. <br> <br> </br>" +
                id +
                @"</div></body></html>";
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

        private bool IsValidRequest(HttpContext context)
        {
            if (!Config.Enabled || context.Request.HttpMethod.ToUpper() != "GET")
            {
                return true;
            }
            string ignoreUrlRegex = Config.IgnoreUrlRegex;
            if (!string.IsNullOrEmpty(ignoreUrlRegex) && Regex.IsMatch(context.Request.RawUrl, ignoreUrlRegex))
            {
                PxModuleEventSource.Log.IgnoreRequest(context.Request.RawUrl, "regex");
                return true;
            }

            // validae using risk cookie
            RiskCookie riskCookie;
            var reason = CheckValidCookie(context, out riskCookie);
            if (reason == RiskRequestReasonEnum.NONE)
            {
                context.Items[UUID_ITEM_KEY] = riskCookie.Uuid;
                // valid cookie, check if to block or not
                if (IsBlockCookieScore(riskCookie.Scores))
                {
                    PxModuleEventSource.Log.RequestBlocked(context.Request.RawUrl, "Cookie", riskCookie.Uuid);
                    return false;
                }
                return true;
            }

            // validate using server risk api
            var risk = FetchGetRisk(context, reason);
            if (risk == null)
            {
                return true;
            }
            if (risk.Scores != null && IsBlockingRequestScore(risk.Scores))
            {
                context.Items[UUID_ITEM_KEY] = risk.Uuid;
                PxModuleEventSource.Log.RequestBlocked(context.Request.RawUrl, "Server API", risk.Uuid);
                return false;
            }
            return true;
        }

        private bool IsBlockingRequestScore(Dictionary<string, int> scores)
        {
            if (scores == null)
            {
                throw new ArgumentNullException("scores");
            }
            var config = Config;
            int botScore;
            if (scores.TryGetValue("non_human", out botScore) && (botScore >= config.BlockScore))
            {
                return true;
            }
            int filterScore;
            if (scores.TryGetValue("filter", out filterScore) && (filterScore >= config.BlockScore))
            {
                return true;
            }
            return false;
        }

        private static RiskRequestHeader[] GetHeadersRiskRequestFormat(NameValueCollection requestHeaders)
        {
            var headers = new RiskRequestHeader[requestHeaders.Count];
            for (int i = 0; i < headers.Length; i++)
            {
                headers[i] = new RiskRequestHeader
                {
                    Name = requestHeaders.GetKey(i),
                    Value = requestHeaders.Get(i)
                };
            }
            return headers;
        }

        private RiskResponse FetchGetRisk(HttpContext context, RiskRequestReasonEnum reason)
        {
            string riskUri = Config.BaseUri + @"/api/v1/risk";
            try
            {
                RiskRequest riskRequest = new RiskRequest
                {
                    Request = new RiskRequestRequest
                    {
                        IP = GetSocketIP(context),
                        Uri = context.Request.RawUrl,
                        Headers = GetHeadersRiskRequestFormat(context.Request.Headers)
                    },
                    Additional = new RiskRequestAdditional
                    {
                        CallReason = reason
                    }
                };

                var riskRequestJson = JsonConvert.SerializeObject(riskRequest);
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, riskUri);
                requestMessage.Content = new StringContent(riskRequestJson, Encoding.UTF8, "application/json");
                var response = httpClient.SendAsync(requestMessage).Result;
                response.EnsureSuccessStatusCode();
                var contentJson = response.Content.ReadAsStringAsync().Result;
                var riskResponse = JsonConvert.DeserializeObject<RiskResponse>(contentJson);
                return riskResponse;
            }
            catch (Exception ex)
            {
                PxModuleEventSource.Log.FailedRiskApi(context.Request.RawUrl, ex.Message);
            }
            return null;
        }

        private bool IsBlockCookieScore(Dictionary<string, int> scores)
        {
            int botScore;
            return (scores.TryGetValue("b", out botScore) && (botScore >= Config.BlockScore));
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


        public static RiskCookie ParseRiskCookie(string cookieData, bool encrypted, string cookieKey = null)
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
            return JsonConvert.DeserializeObject<RiskCookie>(cookieJson);
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
                HttpCookie pxcookie = context.Request.Cookies.Get(config.CookieName);
                if (pxcookie == null)
                {
                    PxModuleEventSource.Log.NoRiskCookie(context.Request.RawUrl);
                    return RiskRequestReasonEnum.NO_COOKIE;
                }
                // parse cookie
                riskCookie = ParseRiskCookie(pxcookie.Value, config.EncryptedCookie, config.CookieKey);
                // check if expired
                if (IsRiskCookieExpired(riskCookie))
                {
                    PxModuleEventSource.Log.CookieExpired(context.Request.RawUrl);
                    return RiskRequestReasonEnum.EXPIRED_COOKIE;
                }

                if (string.IsNullOrEmpty(riskCookie.Hash))
                {
                    PxModuleEventSource.Log.InvalidCookie(context.Request.RawUrl, "missing signature");
                    return RiskRequestReasonEnum.INVALID_COOKIE;
                }

                string expectedHash = CalcCookieHash(context, riskCookie);
                if (expectedHash != riskCookie.Hash)
                {
                    PxModuleEventSource.Log.InvalidCookie(context.Request.RawUrl, "invalid signature");
                    return RiskRequestReasonEnum.INVALID_COOKIE;
                }

                return RiskRequestReasonEnum.NONE;
            }
            catch (Exception ex)
            {
                PxModuleEventSource.Log.InvalidCookie(context.Request.RawUrl, ex.Message);
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
            var keys = riskCookie.Scores.Keys.ToList();
            keys.Sort();
            foreach (var scoreKey in keys)
            {
                int score;
                if (riskCookie.Scores.TryGetValue(scoreKey, out score))
                {
                    sb.Append(score);
                }
            }
            // socket ip
            if (Config.SignWithSocketIp)
            {
                string ip = GetSocketIP(context);
                if (ip != null)
                {
                   sb.Append(ip);
                }
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
            if (Config.SignWithUserAgent)
            {
                var userAgent = context.Request.Headers["user-agent"];
                if (!string.IsNullOrEmpty(userAgent))
                {
                    return userAgent;
                }
            }
            return "";
        }

        private string GetSocketIP(HttpContext context)
        {
            var socketIpHeader = Config.SocketIpHeader;
            if (string.IsNullOrEmpty(socketIpHeader))
            {
                return context.Request.UserHostAddress;
            }
            var ip = context.Request.Headers[socketIpHeader];
            return (ip != null) ? ip.Trim() : string.Empty;
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

