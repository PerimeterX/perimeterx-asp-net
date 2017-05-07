using System.Text;

namespace PerimeterX.DataContracts.Cookies
{
    public class PxCookieV1 : BasePxCookie<DecodedCookieV1>
    {
        public PxCookieV1(PxModuleConfigurationSection config, PxContext context, ICookieDecoder cookieDecoder) : base(config, context, cookieDecoder)
        {
            RawCookie = context.getPxCookie();
        }

        public override string GetDecodedCookieHMAC()
        {
            return DecodedCookie.Hmac;
        }

        public override bool IsSecured()
        {
            string basicHmac = new StringBuilder()
                .Append(DecodedCookie.GetTimestamp())
                .Append(DecodedCookie.Score.Application)
                .Append(DecodedCookie.Score.Bot)
                .Append(DecodedCookie.GetUUID())
                .Append(DecodedCookie.GetVID())
                .ToString();
            string hmacWithIp = new StringBuilder()
                .Append(basicHmac)
                .Append(PxContext.Ip)
                .Append(PxContext.UserAgent)
                .ToString();
            string hmacWithoutIp = new StringBuilder()
                .Append(basicHmac)
                .Append(PxContext.UserAgent)
                .ToString();
            return IsHMACValid(hmacWithoutIp, GetDecodedCookieHMAC()) || IsHMACValid(hmacWithIp, GetDecodedCookieHMAC());

        }
    }
}
