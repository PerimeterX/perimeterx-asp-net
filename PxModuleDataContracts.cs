using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace PerimeterX
{
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
        public Dictionary<string, int> Scores;
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
        public string Uri;

        [DataMember(Name = "headers")]
        public RiskRequestHeader[] Headers;
    }

    [DataContract]
    public enum RiskRequestReasonEnum
    {
        [EnumMember(Value = "none")]
        NONE,
        [EnumMember(Value = "no_cookie")]
        NO_COOKIE,
        [EnumMember(Value = "expired_cookie")]
        EXPIRED_COOKIE,
        [EnumMember(Value = "invalid_cookie")]
        INVALID_COOKIE
    }

    [DataContract]
    public class RiskRequestAdditional
    {
        [DataMember(Name = "s2s_call_reason")]
        [JsonConverter(typeof(StringEnumConverter))]
        public RiskRequestReasonEnum CallReason;
    }

    [DataContract]
    public class RiskRequest
    {
        [DataMember(Name = "cid", EmitDefaultValue = false)]
        public string Cid;

        [DataMember(Name = "request")]
        public RiskRequestRequest Request;

        [DataMember(Name = "additional", EmitDefaultValue = false)]
        public RiskRequestAdditional Additional;
    }

    [DataContract]
    public class RiskResponse
    {
        [DataMember(Name = "uuid")]
        public string Uuid;

        [DataMember(Name = "scores")]
        public Dictionary<string, int> Scores;
    }
}
