using System.Runtime.Serialization;

namespace PerimeterX
{
    public class DecodedCookieV3 : BaseDecodedCookie
    {
        [DataMember(Name = "u")]
        public string Uuid { get; set; }
        [DataMember(Name = "v")]
        public string Vid { get; set; }
        [DataMember(Name = "t")]
        public double Time { get; set; }
        [DataMember(Name = "s")]
        public double Score;
        [DataMember(Name = "a")]
        public string Action;
    }
}
