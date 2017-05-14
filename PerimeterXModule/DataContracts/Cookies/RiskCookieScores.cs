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
}
