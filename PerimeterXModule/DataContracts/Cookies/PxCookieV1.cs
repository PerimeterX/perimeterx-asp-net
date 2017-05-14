using System;
using System.Text;

namespace PerimeterX.DataContracts.Cookies
{
    public class PxCookieV1 : IPxCookie
    {
        private DecodedCookieV1 data;
        private ICookieDecoder cookieDecoder;
        private string rawCookie;

        double IPxCookie.Score
        {
            get
            {
                return data.Score.Bot;
            }
        }

        public string BlockAction
        {
            get
            {
                return "c";
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

        public string Hmac
        {
            get
            {
                return data.Hmac;
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

        public PxCookieV1(ICookieDecoder cookieDecoder, string rawCookie)
        {
            this.rawCookie = rawCookie;
            this.cookieDecoder = cookieDecoder;
        }

        public bool IsSecured(string userAgent, string cookieKey, bool signedWithIP = false, string ip = "")
        {
            var sb = new StringBuilder()
                .Append(data.Time)
                .Append(data.Score.Application)
                .Append(data.Score.Bot)
                .Append(data.Uuid)
                .Append(data.Vid);
            if (signedWithIP)
            {
                sb.Append(ip);
            }
            sb.Append(userAgent);
            return PxCookieUtils.IsHMACValid(cookieKey, sb.ToString(), Hmac);
        }

        public bool Deserialize()
        {
            data = PxCookieUtils.Deserialize<DecodedCookieV1>(cookieDecoder, rawCookie);
            return data != null;
        }

        public bool IsExpired(double time)
        {
            double now = DateTime.UtcNow
                            .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                            .TotalMilliseconds;
            return time < now;
        }
    }
}
