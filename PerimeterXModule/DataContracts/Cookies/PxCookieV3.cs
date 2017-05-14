using System.Text;

namespace PerimeterX.DataContracts.Cookies
{
    public class PxCookieV3 : IPxCookie
    {
        private DecodedCookieV3 data;
        private ICookieDecoder cookieDecoder;
        private string rawCookie;

        public string Hmac { set; get; }

        public double Score
        {
            get
            {
                return data.Score;
            }
        }

        public string Uuid
        {
            get
            {
                return data.Uuid;
            }
        }

        public string Vid
        {
            get
            {
                return data.Vid;
            }
        }

        public string BlockAction
        {
            get
            {
                return data.Action;
            }
        }

        public double Timestamp
        {
            get
            {
                return data.Time;
            }
        }

        public BaseDecodedCookie DecodedCookie
        {
            get
            {
                return data;
            }
        }

        public PxCookieV3(ICookieDecoder cookieDecoder, string rawCookie)
        {
            this.cookieDecoder = cookieDecoder;
            string[] SplitedRawCookie =  rawCookie.Split(new char[] { ':' }, 2);
            if (SplitedRawCookie.Length == 2)
            {
                this.rawCookie = SplitedRawCookie[1];
                Hmac = SplitedRawCookie[0];
            }
        }

        public bool IsSecured(string userAgent, string cookieKey, bool signedWithIP = false, string ip = "")
        {
            string hmacString = new StringBuilder()
                .Append(rawCookie)
                .Append(userAgent)
                .ToString();
            return PxCookieUtils.IsHMACValid(cookieKey, hmacString, Hmac);
        }

        public bool Deserialize()
        {
            data = PxCookieUtils.Deserialize<DecodedCookieV3>(cookieDecoder, rawCookie);
            return data != null;
        }

        public bool IsExpired(double time)
        {
            return IsExpired(data.Time);
        }
    }
}
