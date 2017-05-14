using System.Runtime.Serialization;

namespace PerimeterX
{
    public class DecodedCookieV1 : BaseDecodedCookie
    {
        [DataMember(Name = "u")]
        public string Uuid { get; set; }
        [DataMember(Name = "v")]
        public string Vid { get; set; }
        [DataMember(Name = "t")]
        public double Time { get; set; }
        [DataMember(Name = "h")]
        public string Hmac { get; set; }
        [DataMember(Name = "s")]
        public RiskCookieScores Score { get; set; }
    }
}
