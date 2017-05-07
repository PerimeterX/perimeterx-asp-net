using System.Text;

namespace PerimeterX.DataContracts.Cookies
{
    public class PxCookieV3 : BasePxCookie<DecodedCookieV3>
    {
        public string Hmac { set; get; }

        public PxCookieV3(PxModuleConfigurationSection config, PxContext context, ICookieDecoder cookieDecoder) : base(config, context, cookieDecoder)
        {
            string[] SplitedRawCookie = context.getPxCookie().Split(new char[] { ':' }, 2);
            if (SplitedRawCookie.Length == 2)
            {
                RawCookie = SplitedRawCookie[1];
                Hmac = SplitedRawCookie[0];
            }
        }

        public override bool IsSecured()
        {
            string hmacString = new StringBuilder()
                .Append(RawCookie)
                .Append(PxContext.UserAgent)
                .ToString();
            return IsHMACValid(hmacString, GetDecodedCookieHMAC());
        }

        public override string GetDecodedCookieHMAC()
        {
            return Hmac;
        }
    }
}
