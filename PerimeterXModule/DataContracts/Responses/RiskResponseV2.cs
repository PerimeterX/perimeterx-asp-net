using System;
using System.Runtime.Serialization;

namespace PerimeterX
{
    [DataContract]
    public class RiskResponseV2
    {
        [DataMember(Name = "status")]
        public int Status;

        [DataMember(Name = "message")]
        public string Message;

        [DataMember(Name = "uuid")]
        public string Uuid;

        [DataMember(Name = "scores")]
        public double Score;

        [DataMember(Name = "action")]
        public string RiskResponseAction;

        [DataMember(Name = "error_msg")]
        public string ErrorMessage;
    }
}
