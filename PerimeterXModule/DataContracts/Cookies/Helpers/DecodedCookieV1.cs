using System.Runtime.Serialization;

namespace PerimeterX
{
    public class DecodedCookieV1 : BaseDecodedCookie
    {

        [DataMember(Name = "h")]
        public string Hmac { get; set; }
        [DataMember(Name = "s")]
        public RiskCookieScores Score { get; set; }

        public override string GetBlockAction()
        {
            return "c";
        }

        public override double GetScore()
        {
            return Score.Bot;
        }

        public override bool IsCookieFormatValid()
        {
            return !string.IsNullOrEmpty(Vid) &&
                 !string.IsNullOrEmpty(Uuid) &&
                 !string.IsNullOrEmpty(Hmac) &&
                 Score != null &&
                 !double.IsNaN(Time);

        }
    }
}
