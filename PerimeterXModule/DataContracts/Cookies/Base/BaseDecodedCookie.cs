using System.Runtime.Serialization;

namespace PerimeterX
{
    public abstract class BaseDecodedCookie
    {

        [DataMember(Name = "u")]
        public string Uuid { get; set; }
        [DataMember(Name = "v")]
        public string Vid { get; set; }
        [DataMember(Name = "t")]
        public double Time { get; set; }

        public abstract string GetBlockAction();
        public abstract double GetScore();
        public abstract bool IsCookieFormatValid();

        public string GetUUID()
        {
            return Uuid;
        }
        public string GetVID()
        {
            return Vid;
        }

        public double GetTimestamp()
        {
            return Time;
        }
    }
}
