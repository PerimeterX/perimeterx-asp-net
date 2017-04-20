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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;

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

        [ConfigurationProperty("baseUri", DefaultValue = "https://sapi-{0}.perimeterx.net")]
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

        [ConfigurationProperty("apiTimeout", DefaultValue = 1000)]
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

        [ConfigurationProperty("reporterApiTimeout", DefaultValue = 5000)]
        public int ReporterApiTimeout
        {
            get
            {
                return (int)this["reporterApiTimeout"];
            }
            set
            {
                this["reporterApiTimeout"] = value;
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

        [ConfigurationProperty("suppressContentBlock", DefaultValue = false)]
        public bool SuppressContentBlock
        {
            get
            {
                return (bool)this["suppressContentBlock"];
            }
            set
            {
                this["suppressContentBlock"] = value;
            }
        }

        [ConfigurationProperty("activitiesCapacity", DefaultValue = 512)]
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


        [ConfigurationProperty("fileExtWhitelist", DefaultValue = ".axd,.css,.bmp,.tif,.ttf,.docx,.woff2,.js,.pict,.tiff,.eot,.xlsx,.jpg,.csv,.eps,.woff,.xls,.jpeg,.doc,.ejs,.otf,.pptx,.gif,.pdf,.swf,.svg,.ps,.ico,.pls,.midi,.svgz,.class,.png,.ppt,.mid,.jar")]
        [TypeConverter(typeof(CommaDelimitedStringCollectionConverter))]
        public StringCollection FileExtWhitelist
        {
            get
            {
                return (StringCollection)this["fileExtWhitelist"];
            }
            set
            {
                this["fileExtWhitelist"] = value;
            }
        }

        [ConfigurationProperty("routesWhitelist")]
        [TypeConverter(typeof(CommaDelimitedStringCollectionConverter))]
        public StringCollection RoutesWhitelist
        {
            get
            {
                return (StringCollection)this["routesWhitelist"];
            }
            set
            {
                this["routesWhitelist"] = value;
            }
        }

        [ConfigurationProperty("useragentsWhitelist")]
        [TypeConverter(typeof(CommaDelimitedStringCollectionConverter))]
        public StringCollection UseragentsWhitelist
        {
            get
            {
                return (StringCollection)this["useragentsWhitelist"];
            }
            set
            {
                this["useragentsWhitelist"] = value;
            }
        }

        [ConfigurationProperty("customLogo")]
        public string CustomLogo
        {
            get
            {
                return (string)this["customLogo"];
            }
            set
            {
                this["logoVisibility"] = "visible";
                this["customLogo"] = value;
            }
        }

        [ConfigurationProperty("cssRef")]
        public string CssRef
        {
            get
            {
                return (string)this["cssRef"];
            }
            set
            {
                this["cssRef"] = value;
            }
        }

        [ConfigurationProperty("jsRef")]
        public string JsRef
        {
            get
            {
                return (string)this["jsRef"];
            }
            set
            {
                this["jsRef"] = value;
            }
        }
    }
}
