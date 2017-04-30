using PerimeterX.DataContracts.Cookies.Base;
using System;
using System.Runtime.Serialization;

namespace PerimeterX.DataContracts.Cookies.Helpers
{
    public class DecodedCookieV3 : BaseDecodedCookie
    {
        [DataMember(Name = "s")]
        public double Score;
        [DataMember(Name = "a")]
        public string Action;

        public override string GetBlockAction()
        {
            return Action;
        }

        public override double GetScore()
        {
            return Score;
        }

        public override bool IsCookieFormatValid()
        {
            return !string.IsNullOrEmpty(Vid) &&
                 !string.IsNullOrEmpty(Uuid) &&
                 !string.IsNullOrEmpty(Action) &&
                 !double.IsNaN(Score) &&
                 !double.IsNaN(Time);

        }
    }
}
