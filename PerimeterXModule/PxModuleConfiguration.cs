using System;
using System.Collections.Specialized;
using System.ComponentModel;
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

        [ConfigurationProperty("appId", IsRequired = true)]
        public string AppId
        {
            get
            {
                return (string)this["appId"];
            }
            set
            {
                this["appId"] = value;
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

        [ConfigurationProperty("encryptionEnabled", DefaultValue = true)]
        public bool EncryptionEnabled
        {
            get
            {
                return (bool)this["encryptionEnabled"];
            }
            set
            {
                this["encryptionEnabled"] = value;
            }
        }

        [ConfigurationProperty("captchaEnabled", DefaultValue = true)]
        public bool CaptchaEnabled
        {
            get
            {
                return (bool)this["captchaEnabled"];
            }
            set
            {
                this["captchaEnabled"] = value;
            }
        }

        [ConfigurationProperty("signedWithUserAgent", DefaultValue = true)]
        public bool SignedWithUserAgent
        {
            get
            {
                return (bool)this["signedWithUserAgent"];
            }
            set
            {
                this["signedWithUserAgent"] = value;
            }
        }

        [ConfigurationProperty("signedWithIP", DefaultValue = false)]
        public bool SignedWithIP
        {
            get
            {
                return (bool)this["signedWithIP"];
            }
            set
            {
                this["signedWithIP"] = value;
            }
        }

        [ConfigurationProperty("blockingScore", DefaultValue = 70)]
        public int BlockingScore
        {
            get
            {
                return (int)this["blockingScore"];
            }
            set
            {
                this["blockingScore"] = value;
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

        [ConfigurationProperty("baseUri", DefaultValue = "https://sapi.perimeterx.net")]
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

        [ConfigurationProperty("apiTimeout", DefaultValue = 2000)]
        public int ApiTimeout
        {
            get
            {
                return (int)this["apiTimeout"];
            }
            set
            {
                this["apiTimeout"] = value;
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

        [ConfigurationProperty("activitiesCapacity", DefaultValue = 500)]
        public int ActivitiesCapacity
        {
            get
            {
                return (int)this["activitiesCapacity"];
            }
            set
            {
                this["activitiesCapacity"] = value;
            }
        }

        [ConfigurationProperty("activitiesBulkSize", DefaultValue = 10)]
        public int ActivitiesBulkSize
        {
            get
            {
                return (int)this["activitiesBulkSize"];
            }
            set
            {
                this["activitiesBulkSize"] = value;
            }
        }

        [ConfigurationProperty("sendPageActivities", DefaultValue = false)]
        public bool SendPageActivites
        {
            get
            {
                return (bool)this["sendPageActivities"];
            }
            set
            {
                this["sendPageActivities"] = value;
            }
        }

        [ConfigurationProperty("sendBlockActivities", DefaultValue = true)]
        public bool SendBlockActivites
        {
            get
            {
                return (bool)this["sendBlockActivities"];
            }
            set
            {
                this["sendBlockActivities"] = value;
            }
        }

        [ConfigurationProperty("sensitiveHeaders", DefaultValue = "cookie,cookies")]
        [TypeConverter(typeof(CommaDelimitedStringCollectionConverter))]
        public StringCollection SensitiveHeaders
        {
            get
            {
                return (StringCollection)this["sensitiveHeaders"];
            }
            set
            {
                this["sensitiveHeaders"] = value;
            }
        }

    }
}