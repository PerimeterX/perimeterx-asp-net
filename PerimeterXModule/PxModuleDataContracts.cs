using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.Serialization;

namespace PerimeterX
{
    interface RiskScores
    {
        int Application { get; set; }
        int Bot { get; set; }
    }

    [DataContract]
    public class RiskCookieScores : RiskScores
    {
        [DataMember(Name = "a")]
        public int Application { get; set; }

        [DataMember(Name = "b")]
        public int Bot { get; set; }
    }

    [DataContract]
    public class RiskCookie
    {
        [DataMember(Name = "t")]
        public double Time;

        [DataMember(Name = "h")]
        public string Hash;

        [DataMember(Name = "u")]
        public string Uuid;

        [DataMember(Name = "v")]
        public string Vid;

        [DataMember(Name = "s")]
        public RiskCookieScores Scores;
    }

    [DataContract]
    public class RiskRequestHeader
    {
        [DataMember(Name = "name")]
        public string Name;

        [DataMember(Name = "value")]
        public string Value;
    }

    [DataContract]
    public class RiskRequestRequest
    {
        [DataMember(Name = "ip")]
        public string IP;

        [DataMember(Name = "uri")]
        public string URI;

        [DataMember(Name = "url")]
        public string URL;

        [DataMember(Name = "headers")]
        public RiskRequestHeader[] Headers;
    }

    public enum RiskRequestReasonEnum
    {
        NONE,
        NO_COOKIE,
        EXPIRED_COOKIE,
        INVALID_COOKIE
    }

    [DataContract]
    public class RiskRequestAdditional
    {
        [DataMember(Name = "px_cookie", EmitDefaultValue = false)]
        public string PXCookie;

        [DataMember(Name = "http_method")]
        public string HttpMethod;

        [DataMember(Name = "http_version")]
        public string HttpVersion;

        [DataMember(Name = "module_version")]
        public string ModuleVersion { get { return PxModule.MODULE_VERSION; } set { } }

        [NonSerialized]
        public RiskRequestReasonEnum CallReason;

        [DataMember(Name = "s2s_call_reason")]
        public string S2SCallReason
        {
            get
            {
                switch (CallReason)
                {
                    case RiskRequestReasonEnum.NONE:
                        return "none";
                    case RiskRequestReasonEnum.NO_COOKIE:
                        return "no_cookie";
                    case RiskRequestReasonEnum.EXPIRED_COOKIE:
                        return "expired_cookie";
                    case RiskRequestReasonEnum.INVALID_COOKIE:
                        return "invalid_cookie";
                    default:
                        break;
                }
                return "";
            }
            set
            {
                switch (value)
                {
                    case "no_cookie":
                        CallReason = RiskRequestReasonEnum.NO_COOKIE;
                        break;
                    case "expired_cookie":
                        CallReason = RiskRequestReasonEnum.EXPIRED_COOKIE;
                        break;
                    case "invalid_cookie":
                        CallReason = RiskRequestReasonEnum.INVALID_COOKIE;
                        break;
                    case "none":
                    default:
                        CallReason = RiskRequestReasonEnum.NONE;
                        break;
                }
            }
        }

    }

    [DataContract]
    public class RiskRequest
    {
        [DataMember(Name = "request")]
        public RiskRequestRequest Request;

        [DataMember(Name = "additional", EmitDefaultValue = false)]
        public RiskRequestAdditional Additional;
    }

    [DataContract]
    public class RiskResponseScores : RiskScores
    {
        [DataMember(Name = "suspected_script")]
        public int Application { get; set; }

        [DataMember(Name = "non_human")]
        public int Bot { get; set; }
    }

    [DataContract]
    public class RiskResponse
    {
        [DataMember(Name = "status")]
        public int Status;

        [DataMember(Name = "message")]
        public string Message;

        [DataMember(Name = "uuid")]
        public string Uuid;

        [DataMember(Name = "scores")]
        public RiskResponseScores Scores;
    }

    [DataContract]
    public class Activity
    {
        [DataMember(Name = "type")]
        public string Type;

        [DataMember(Name = "url")]
        public string Url;

        [DataMember(Name = "px_app_id")]
        public string AppId;

        [DataMember(Name = "vid", EmitDefaultValue = false)]
        public string Vid;

        [DataMember(Name = "timestamp")]
        public double Timestamp;

        [DataMember(Name = "socket_ip", EmitDefaultValue = false)]
        public string SocketIP;

        [DataMember(Name = "headers", EmitDefaultValue = false)]
        public Dictionary<string, object> Headers;

        [DataMember(Name = "details", EmitDefaultValue = false)]
        public Dictionary<string, object> Details;
    }

}
