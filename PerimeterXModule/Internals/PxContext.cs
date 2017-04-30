﻿using System.Collections.Generic;
using System.Web;
using System;
using System.Net;

namespace PerimeterX
{
    public class PxContext
    {
        public Dictionary<string, string> PxCookies { get; set; }
        public string DecodedPxCookie { get; set; }
        public string PxCookieHmac { get; set; }
        public string PxCaptcha { get; set; }
        public string Ip { get; set; }
        public string HttpVersion { get; set; }
        public string HttpMethod { get; set; }
        public List<RiskRequestHeader> Headers { get; set; }
        public string Hostname { get; set; }
        public string Uri { get; set; }
        public string UserAgent { get; set; }
        public string FullUrl { get; set; }
        public string S2SCallReason { get; set; }
        public int Score { get; set; }
        public string Vid { get; set; }
        public string UUID { get; set; }
        public BlockReasonEnum BlockReason { get; set; }
        public bool MadeS2SCallReason { get; set; }
        public string S2SHttpErrorMessage { get; set; }
        public string BlockAction { get; set; }
        public string BlockData { get; set; }
        public HttpContext ApplicationContext { get; private set; }

        public PxContext(HttpContext context, PxModuleConfigurationSection pxConfiguration)
        {
            ApplicationContext = context;

            // Get Cookies
            PxCookies = new Dictionary<string, string>();
            var contextCookie = context.Request.Cookies;
            foreach (string key in contextCookie.AllKeys)
            {
                if (Array.IndexOf(PxConstants.PX_COOKIES_PREFIX, key) > -1 )
                {
                    PxCookies.Add(key, contextCookie.Get(key).Value);
                } else if ( key.Equals(PxConstants.COOKIE_CAPTCHA_PREFIX))
                {
                    var captchaCookie = contextCookie.Get(key).Value;
                    var captchaCookieParts = captchaCookie.Split(new char[] { ':' }, 2);
                    if (captchaCookieParts.Length == 2)
                    {
                        PxCaptcha = captchaCookieParts[0];
                        Vid = captchaCookieParts[1];
                        var expiredCookie = new HttpCookie(PxConstants.COOKIE_CAPTCHA_PREFIX) { Expires = DateTime.Now.AddDays(-1) };
                        context.Response.Cookies.Add(expiredCookie);
                    }
                }
            }

            // Get Headers
            Headers = new List<RiskRequestHeader>(context.Request.Headers.Count);
            for (int i = 0; i < context.Request.Headers.Count; i++)
            {
                var key = context.Request.Headers.GetKey(i);
                // Remove HTTP_ prefix
                var header = new RiskRequestHeader
                {
                    Name = key,
                    Value = context.Request.Headers.Get(i)
                };
                Headers.Add(header);
            }

            Hostname = context.Request.UserHostName;

            UserAgent = context.Request.Headers["user-agent"];
            // if userAgentOverride is present override the default user-agent
            string userAgentOverride = pxConfiguration.UserAgentOverride;
            if (!string.IsNullOrEmpty(userAgentOverride))
            {
                UserAgent = context.Request.Headers[userAgentOverride];
            }

            Uri = context.Request.Url.PathAndQuery;
            FullUrl = context.Request.Url.ToString();
            Score = 0;
            BlockReason = BlockReason = BlockReasonEnum.NONE;


            Ip = context.Request.UserHostAddress;
            // Get IP from custom header
            string socketIpHeader = pxConfiguration.SocketIpHeader;
            if (!string.IsNullOrEmpty(socketIpHeader))
            {
                var headerVal = context.Request.Headers[socketIpHeader];
                if (headerVal != null)
                {
                    var ips = headerVal.Split(new char[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    IPAddress firstIpAddress;
                    if (ips.Length > 0 && IPAddress.TryParse(ips[0], out firstIpAddress))
                    {
                        Ip = ips[0];
                    }
                }
            }

            HttpVersion = ExtractHttpVersion(context);
            HttpMethod = context.Request.HttpMethod;


        }

        private string ExtractHttpVersion(HttpContext context)
        {
            string serverProtocol = context.Request.ServerVariables["SERVER_PROTOCOL"];
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

        public string getPxCookie()
        {
            if (PxCookies.Count == 0)
            {
                return null;
            }
            return PxCookies.ContainsKey(PxConstants.PX_COOKIES_PREFIX[0]) ? PxCookies[PxConstants.PX_COOKIES_PREFIX[0]] : PxCookies[PxConstants.PX_COOKIES_PREFIX[1]];
        }


    }
}
