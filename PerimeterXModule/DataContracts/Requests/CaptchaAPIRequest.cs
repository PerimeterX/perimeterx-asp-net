using System.Runtime.Serialization;

namespace PerimeterX 
{
    [DataContract]
    public class CaptchaAPIRequest
    {
        [DataMember(Name = "request")]
        public CaptchaRequest Request;

        [DataMember(Name = "pxCaptcha")]
        public string PXCaptcha;

        [DataMember(Name = "hostname")]
        public string Hostname;

        [DataMember(Name = "additional")]
        public Additional Additional;
    }
}