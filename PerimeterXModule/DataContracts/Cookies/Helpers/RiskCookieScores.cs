using System.Runtime.Serialization;
namespace PerimeterX.DataContracts.Cookies.Helpers
{
    [DataContract]
    public class RiskCookieScores : BaseRiskCookieScores
    {
        [DataMember(Name = "a")]
        public int Application;

        [DataMember(Name = "b")]
        public int Bot;
    }
}
