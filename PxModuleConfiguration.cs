using System;
using System.Configuration;
using System.Net;

namespace PerimeterX
{
    public class PxModuleConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("enabled", DefaultValue = true)]
        public bool Enabled
        {
            get
            {
                return (bool)this["enabled"];
            }
            set
            {
                this["enabled"] = value;
            }
        }

        [ConfigurationProperty("cookieName", DefaultValue = "_px")]
        public string CookieName
        {
            get
            {
                return (string)this["cookieName"];
            }
            set
            {
                this["cookieName"] = value;
            }
        }

        [ConfigurationProperty("cookieKey", IsRequired = true)]
        public string CookieKey
        {
            get
            {
                return (string)this["cookieKey"];
            }
            set
            {
                this["cookieKey"] = value;
            }
        }

        [ConfigurationProperty("encryptedCookie", DefaultValue = true)]
        public bool EncryptedCookie
        {
            get
            {
                return (bool)this["encryptedCookie"];
            }
            set
            {
                this["encryptedCookie"] = value;
            }
        }

        [ConfigurationProperty("signWithUserAgent", DefaultValue = true)]
        public bool SignWithUserAgent
        {
            get
            {
                return (bool)this["signWithUserAgent"];
            }
            set
            {
                this["signWithUserAgent"] = value;
            }
        }

        [ConfigurationProperty("signWithSocketIp", DefaultValue = true)]
        public bool SignWithSocketIp
        {
            get
            {
                return (bool)this["signWithSocketIp"];
            }
            set
            {
                this["signWithSocketIp"] = value;
            }
        }

        [ConfigurationProperty("blockScore", DefaultValue = 0)]
        public int BlockScore
        {
            get
            {
                return (int)this["blockScore"];
            }
            set
            {
                this["blockScore"] = value;
            }
        }

        [ConfigurationProperty("apiToken", DefaultValue = "")]
        public string ApiToken
        {
            get
            {
                return (string)this["apiToken"];
            }
            set
            {
                this["apiToken"] = value;
            }
        }

        [ConfigurationProperty("baseUri", DefaultValue = "https://collector.a.pxi.pub")]
        public string BaseUri
        {
            get
            {
                return (string)this["baseUri"];
            }
            set
            {
                this["baseUri"] = value;
            }
        }

        [ConfigurationProperty("backchannelTimeout", DefaultValue = "0:00:02")]
        public TimeSpan BackchannelTimeout
        {
            get
            {
                return (TimeSpan)this["backchannelTimeout"];
            }
            set
            {
                this["backchannelTimeout"] = value;
            }
        }

        [ConfigurationProperty("socketIpHeader")]
        public string SocketIpHeader
        {
            get
            {
                return (string)this["socketIpHeader"];
            }
            set
            {
                this["socketIpHeader"] = value;
            }
        }

        [ConfigurationProperty("ignoreUrlRegex")]
        public string IgnoreUrlRegex
        {
            get
            {
                return (string)this["ignoreUrlRegex"];
            }
            set
            {
                this["ignoreUrlRegex"] = value;
            }
        }

        [ConfigurationProperty("internalBlockPage", DefaultValue = false)]
        public bool InternalBlockPage
        {
            get
            {
                return (bool)this["internalBlockPage"];
            }
            set
            {
                this["internalBlockPage"] = value;
            }
        }

        [ConfigurationProperty("blockStatusCode", DefaultValue = (int)HttpStatusCode.Forbidden)]
        public int BlockStatusCode
        {
            get
            {
                return (int)this["blockStatusCode"];
            }
            set
            {
                this["blockStatusCode"] = value;
            }
        }
    }
}