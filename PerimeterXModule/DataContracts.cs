// 	Copyright © 2016 PerimeterX, Inc.
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
using System.Runtime.Serialization;

namespace PerimeterX
{
    [DataContract]
    public class RiskCookieScores
    {
        [DataMember(Name = "a")]
        public int Application;

        [DataMember(Name = "b")]
        public int Bot;
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
    public class Request
    {
        [DataMember(Name = "ip")]
        public string IP;

        [DataMember(Name = "url")]
        public string URL;

        [DataMember(Name = "headers")]
        public RiskRequestHeader[] Headers;
    }

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
    public class Additional
    {
        [DataMember(Name = "px_cookie", EmitDefaultValue = false)]
        public string PXCookie;

        [DataMember(Name = "http_method")]
        public string HttpMethod;

        [DataMember(Name = "http_version")]
        public string HttpVersion;

        [DataMember(Name = "module_version")]
        public string ModuleVersion { get { return PxModule.MODULE_VERSION; } set { } }

        [DataMember(Name = "s2s_call_reason")]
        public RiskRequestReasonEnum CallReason;
    }

    [DataContract]
    public class RiskRequest
    {
        [DataMember(Name = "request")]
        public Request Request;

        [DataMember(Name = "vid", EmitDefaultValue = false)]
        public string Vid;

        [DataMember(Name = "additional", EmitDefaultValue = false)]
        public Additional Additional;
    }

    [DataContract]
    public class RiskResponseScores
    {
        [DataMember(Name = "filter")]
        public int Filter;

        [DataMember(Name = "suspected_script")]
        public int Application;

        [DataMember(Name = "non_human")]
        public int Bot;
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
    public class CaptchaRequest
    {
        [DataMember(Name = "request")]
        public Request Request;

        [DataMember(Name = "vid", EmitDefaultValue = false)]
        public string Vid;

        [DataMember(Name = "pxCaptcha")]
        public string PXCaptcha;

        [DataMember(Name = "hostname")]
        public string Hostname;
    }

    [DataContract]
    public class CaptchaResponse
    {
        [DataMember(Name = "status")]
        public int Status;

        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message;

        [DataMember(Name = "uuid")]
        public string Uuid;

        [DataMember(Name = "vid")]
        public string Vid;

        [DataMember(Name = "cid")]
        public string Cid;
    }

    [DataContract]
    public enum BlockReasonEnum
    {
        [EnumMember(Value = "none")]
        NONE,
        [EnumMember(Value = "cookie_high_score")]
        COOKIE_HIGH_SCORE,
        [EnumMember(Value = "risk_high_score")]
        RISK_HIGH_SCORE
    }

    [DataContract]
    public class ActivityDetails
    {
        [DataMember(Name = "block_reason")]
        public BlockReasonEnum BlockReason;

        [DataMember(Name = "block_uuid")]
        public string BlockUuid;
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
        public Dictionary<string, string> Headers;

        [DataMember(Name = "details", EmitDefaultValue = false)]
        public ActivityDetails Details;
    }

}
