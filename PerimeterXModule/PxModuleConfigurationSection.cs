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
using System.Collections.Generic;
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

        [ConfigurationProperty("challengeEnabled", DefaultValue = true)]
        public bool ChallengeEnabled
        {
            get
            {
                return (bool)this["challengeEnabled"];
            }
            set
            {
                this["challengeEnabled"] = value;
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

        [ConfigurationProperty("blockingScore", DefaultValue = 100)]
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

        [ConfigurationProperty("apiTimeout", DefaultValue = 1500)]
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

        [ConfigurationProperty("sendPageActivities", DefaultValue = true)]
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

        [ConfigurationProperty("useragentOverride")]
        public string UserAgentOverride
        {
            get
            {
                return (string)this["useragentOverride"];
            }
            set
            {
                this["useragentOverride"] = value;
            }
        }

        [ConfigurationProperty("monitorMode", DefaultValue = true)]
        public bool MonitorMode
        {
            get
            {
                return (bool)this["monitorMode"];
            }
            set
            {
                this["monitorMode"] = value;
            }
        }

        [ConfigurationProperty("sensitiveRoutes")]
        [TypeConverter(typeof(CommaDelimitedStringCollectionConverter))]
        public StringCollection SensitiveRoutes
        {
            get
            {
                return (StringCollection)this["sensitiveRoutes"];
            }
            set
            {
                this["sensitiveRoutes"] = value;
            }
        }

        [ConfigurationProperty("customVerificationHandler")]
        public string CustomVerificationHandler
        {
            get
            {
                return (string)this["customVerificationHandler"];
            }
            set
            {
                this["customVerificationHandler"] = value;
            }
        }

        [ConfigurationProperty("collectorUrl", DefaultValue = "https://collector-{0}.perimeterx.net")]
        public string CollectorUrl
        {
            get
            {
                return (string)this["collectorUrl"];
            }
            set
            {
                this["collectorUrl"] = value;
            }
        }

        [ConfigurationProperty("enforceSpecificRoutes")]
        [TypeConverter(typeof(CommaDelimitedStringCollectionConverter))]
        public StringCollection EnforceSpecificRoutes
        {
            get
            {
                return (StringCollection)this["enforceSpecificRoutes"];
            }
            set
            {
                this["enforceSpecificRoutes"] = value;
            }
        }

        [ConfigurationProperty("firstPartyEnabled", DefaultValue = true)]
        public bool FirstPartyEnabled
        {
            get
            {
                return (bool)this["firstPartyEnabled"];
            }
            set
            {
                this["firstPartyEnabled"] = value;
            }
        }

        [ConfigurationProperty("firstPartyXhrEnabled", DefaultValue = true)]
        public bool FirstPartyXhrEnabled
        {
            get
            {
                return (bool)this["firstPartyXhrEnabled"];
            }
            set
            {
                this["firstPartyXhrEnabled"] = value;
            }
        }

        [ConfigurationProperty("clientHostUrl", DefaultValue = "https://client.perimeterx.net")]
        public string ClientHostUrl
        {
            get
            {
                return (string)this["clientHostUrl"];
            }

            set
            {
                this["clientHostUrl"] = value;
            }
        }

        [ConfigurationProperty("captchaHostUrl", DefaultValue = "https://captcha.perimeterx.net")]
        public string CaptchaHostUrl
        {
            get
            {
                return (string)this["captchaHostUrl"];
            }

            set
            {
                this["captchaHostUrl"] = value;
            }
        }

        [ConfigurationProperty("customBlockUrl", DefaultValue = null)]
        public string CustomBlockUrl
        {
            get
            {
                return (string)this["customBlockUrl"];
            }

            set
            {
                this["customBlockUrl"] = value;
            }
        }

        [ConfigurationProperty("redirectOnCustomUrl", DefaultValue = false)]
        public bool RedirectOnCustomUrl
        {
            get
            {
                return (bool)this["redirectOnCustomUrl"];
            }

            set
            {
                this["redirectOnCustomUrl"] = value;
            }
        }

        [ConfigurationProperty("mitigationUrls", DefaultValue = "")]
        [TypeConverter(typeof(CommaDelimitedStringCollectionConverter))]
        public StringCollection MitigationUrls
        {
            get
            {
                return (StringCollection) this["mitigationUrls"];
            }

            set
            {
                this["mitigationUrls"] = value;
            }
        }

        [ConfigurationProperty("bypassMonitorHeader", DefaultValue = "")]
        public string ByPassMonitorHeader
        {
            get
            {
                return (string)this["bypassMonitorHeader"];
            }

            set
            {
                this["bypassMonitorHeader"] = value;
            }
        }

        [ConfigurationProperty("loginCredentialsExtractionEnabled", DefaultValue = false)]
        public bool LoginCredentialsExtractionEnabled
        {
            get
            {
                return (bool)this["loginCredentialsExtractionEnabled"];
            }

            set
            {
                this["loginCredentialsExtractionEnabled"] = value;
            }
        }

        [ConfigurationProperty("loginCredentialsExtraction", DefaultValue = "")]
        public string LoginCredentialsExtraction
        {
            get
            {
                return (string)this["loginCredentialsExtraction"];
            }

            set
            {
                this["loginCredentialsExtraction"] = value;
            }

        }
    

        [ConfigurationProperty("ciVersion", DefaultValue = "v2")]
        public string CiVersion
        {
            get
            {
                return (string)this["ciVersion"];
            }

            set
            {
                this["ciVersion"] = value;
            }

        }

        [ConfigurationProperty("compromisedCredentialsHeader", DefaultValue = "px-compromised-credentials")]
        public string CompromisedCredentialsHeader
        {
            get
            {
                return (string)this["compromisedCredentialsHeader"];
            }

            set
            {
                this["compromisedCredentialsHeader"] = value;
            }

        }  
        
        [ConfigurationProperty("sendRawUsernameOnAdditionalS2SActivity", DefaultValue = false)]
        public bool SendRawUsernameOnAdditionalS2SActivity
        {
            get
            {
                return (bool)this["sendRawUsernameOnAdditionalS2SActivity"];
            }

            set
            {
                this["sendRawUsernameOnAdditionalS2SActivity"] = value;
            }

        } 
        
        [ConfigurationProperty("additionalS2SActivityHeaderEnabled", DefaultValue = false)]
        public bool AdditionalS2SActivityHeaderEnabled
        {
            get
            {
                return (bool)this["additionalS2SActivityHeaderEnabled"];
            }

            set
            {
                this["additionalS2SActivityHeaderEnabled"] = value;
            }

        }


        [ConfigurationProperty("loginSuccessfulReportingMethod", DefaultValue = "")]
        public string LoginSuccessfulReportingMethod
        {
            get
            {
                return (string)this["loginSuccessfulReportingMethod"];
            }

            set
            {
                this["loginSuccessfulReportingMethod"] = value;
            }

        } 
        
        [ConfigurationProperty("loginSuccessfulBodyRegex", DefaultValue = "")]
        public string LoginSuccessfulBodyRegex
        {
            get
            {
                return (string)this["loginSuccessfulBodyRegex"];
            }

            set
            {
                this["loginSuccessfulBodyRegex"] = value;
            }

        }

        [ConfigurationProperty("loginSuccessfulHeaderName", DefaultValue = "")]
        public string LoginSuccessfulHeaderName
        {
            get
            {
                return (string)this["loginSuccessfulHeaderName"];
            }

            set
            {
                this["loginSuccessfulHeaderName"] = value;
            }

        }

        [ConfigurationProperty("loginSuccessfulHeaderValue", DefaultValue = "")]
        public string LoginSuccessfulHeaderValue
        {
            get
            {
                return (string)this["loginSuccessfulHeaderValue"];
            }

            set
            {
                this["loginSuccessfulHeaderValue"] = value;
            }

        }

        [ConfigurationProperty("loginSuccessfulStatus", DefaultValue = "")]
        [TypeConverter(typeof(CommaDelimitedStringCollectionConverter))]
        public StringCollection LoginSuccessfulStatus
        {
            get
            {
                return (StringCollection)this["loginSuccessfulStatus"];
            }

            set
            {
                this["loginSuccessfulStatus"] = value;
            }

        }
    }
}
