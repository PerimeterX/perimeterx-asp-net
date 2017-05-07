using Jil;
using System;
using System.Security.Cryptography;
using System.Text;

namespace PerimeterX
{
    public abstract class BasePxCookie<T> : IPxCookie where T : BaseDecodedCookie
    {
        public PxModuleConfigurationSection Config { get; set; }
        public string CookieKey { get; set; }
        public T DecodedCookie { get; set; }
        public ICookieDecoder CookieDecoder;
        public PxContext PxContext { get; set; }
        public string RawCookie { get; set; }

        public BasePxCookie(PxModuleConfigurationSection config, PxContext context, ICookieDecoder cookieDecoder)
        {
            Config = config;
            PxContext = context;
            CookieDecoder = cookieDecoder;
        }

        public bool Deserialize()
        {
            string cookieString = CookieDecoder.Decode(RawCookie);
            if (string.IsNullOrEmpty(cookieString))
            {
                return false;
            }

            DecodedCookie = JSON.Deserialize<T>(cookieString, PxConstants.JSON_OPTIONS);

            return !DecodedCookie.IsCookieFormatValid();
        }

        public bool IsCookieHighScore()
        {
            return DecodedCookie.GetScore() >= this.Config.BlockingScore;
        }

        public bool IsExpired()
        {
            double now = DateTime.UtcNow
                            .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                            .TotalMilliseconds;
            return DecodedCookie.GetTimestamp() < now;
        }

        public bool IsHMACValid(string UncodedHmac, string CookieHmac)
        {
            var cookieKeyBytes = Encoding.UTF8.GetBytes(Config.CookieKey);
            var hash = new HMACSHA256(cookieKeyBytes);
            var expectedHashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(UncodedHmac));
            var encodedHmac = this.ByteArrayToHexString(expectedHashBytes);

            return encodedHmac == CookieHmac;
        }

        private string ByteArrayToHexString(byte[] input)
        {
            StringBuilder sb = new StringBuilder(input.Length * 2);
            foreach (byte b in input)
            {
                sb.Append(PxConstants.HEX_ALPHABET[b >> 4]);
                sb.Append(PxConstants.HEX_ALPHABET[b & 0xF]);
            }
            return sb.ToString();
        }

        public BaseDecodedCookie GetDecodedCookie()
        {
            return DecodedCookie;
        }

        public abstract bool IsSecured();
        public abstract string GetDecodedCookieHMAC();

    }

}
