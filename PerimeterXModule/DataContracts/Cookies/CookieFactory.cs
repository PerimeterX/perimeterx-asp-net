using PerimeterX.DataContracts.Cookies.Interface;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PerimeterX.DataContracts.Cookies
{
    public static class CookieFactory
    {
        public static IPxCookie BuildCookie(PxModuleConfigurationSection config, PxContext context, ICookieDecoder cookieDecoder)
        {
            List<string> PxCookiesKeys = new List<string>(context.PxCookies.Keys);
            if (PxCookiesKeys.Count > 0)
            {
                PxCookiesKeys.Sort();
                string Key = PxCookiesKeys[0];

                switch (Key)
                {
                    case PxConstants.COOKIE_V1_PREFIX:
                        return new PxCookieV1(config, context, cookieDecoder);
                    case PxConstants.COOKIE_V3_PREFIX:
                        return new PxCookieV3(config, context, cookieDecoder);
                }
            }
            return null;
        }
    }
}
