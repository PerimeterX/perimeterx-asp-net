using System.Linq;
using System;

namespace PerimeterX.DataContracts.Cookies
{
    public static class CookieFactory
    {
        public static IPxCookie BuildCookie(PxModuleConfigurationSection config, PxContext context, ICookieDecoder cookieDecoder)
        {
            string[] PxCookiesKeys = new string[context.PxCookies.Keys.Count()];
            context.PxCookies.Keys.CopyTo(PxCookiesKeys,0);

            if (PxCookiesKeys.Length > 0)
            {
                Array.Sort(PxCookiesKeys, new Comparison<string>((i1, i2) => i2.CompareTo(i1)));// descending sort;
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
