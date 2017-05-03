using System;
using System.Runtime.Serialization;

namespace PerimeterX
{
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
}
