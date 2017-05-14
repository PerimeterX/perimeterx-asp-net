using Jil;
using PerimeterX.DataContracts.Cookies;
using System;
using System.Security.Cryptography;
using System.Text;

namespace PerimeterX
{
    public static class PxCookieUtils
    {
        public static IPxCookie BuildCookie(PxModuleConfigurationSection config, PxContext context, ICookieDecoder cookieDecoder)
        {
            if (context.PxCookies.Count > 0)
            {
                var cookie = context.PxCookies.ContainsKey(PxConstants.COOKIE_V3_PREFIX) ? context.PxCookies[PxConstants.COOKIE_V3_PREFIX] : null;
                if (cookie != null)
                {
                    return new PxCookieV3(cookieDecoder, cookie);
                }
                cookie = context.PxCookies[PxConstants.COOKIE_V1_PREFIX];
                if (cookie != null)
                {
                    return new PxCookieV1(cookieDecoder, cookie);
                }
            }
            return null;
        }

        public static T Deserialize<T>(ICookieDecoder cookieDecoder, string rawCookie)
        {
            string cookieString = cookieDecoder.Decode(rawCookie);
            if (string.IsNullOrEmpty(cookieString))
            {
                return default(T);
            }

            return JSON.Deserialize<T>(cookieString, PxConstants.JSON_OPTIONS);
        }

        public static bool IsExpired(double date)
        {
            double now = DateTime.UtcNow
                            .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                            .TotalMilliseconds;
            return date < now;
        }

        public static bool IsHMACValid(string cookieKey, string UncodedHmac, string CookieHmac)
        {
            var cookieKeyBytes = Encoding.UTF8.GetBytes(cookieKey);
            var hash = new HMACSHA256(cookieKeyBytes);
            var expectedHashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(UncodedHmac));
            var encodedHmac = ByteArrayToHexString(expectedHashBytes);
            return encodedHmac == CookieHmac;
        }

        public static string ByteArrayToHexString(byte[] input)
        {
            StringBuilder sb = new StringBuilder(input.Length * 2);
            foreach (byte b in input)
            {
                sb.Append(PxConstants.HEX_ALPHABET[b >> 4]);
                sb.Append(PxConstants.HEX_ALPHABET[b & 0xF]);
            }
            return sb.ToString();
        }
    }

}
