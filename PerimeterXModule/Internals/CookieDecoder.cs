using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace PerimeterX
{
	public interface ICookieDecoder
	{
		string Decode(string cookieData);
	}

	public class CookieDecoder : ICookieDecoder
	{
		public string Decode(string cookie)
		{
			byte[] bytes = Convert.FromBase64String(cookie);
			return (bytes != null) ? Encoding.UTF8.GetString(bytes) : string.Empty;
		}
	}

    public class EncryptedCookieDecoder : ICookieDecoder
    {
        private PXConfigurationWrapper pxConfig;

        private const int KEY_SIZE_BITS = 256;
		private const int IV_SIZE_BITS = 128;

        public EncryptedCookieDecoder(PXConfigurationWrapper pxConfig)
		{
			if (pxConfig == null)
			{
				throw new NullReferenceException("PXConfigurationWrapper");
			}
            this.pxConfig = pxConfig;
		}

		public string Decode(string cookie)
		{
            var cookieKeyBytes = Encoding.UTF8.GetBytes(pxConfig.CookieKey);
			if (cookie == null)
			{
				throw new ArgumentNullException("cookie");
			}
			if (cookie.Length > 2048)
			{
				throw new ArgumentOutOfRangeException("cookie", "length exceeded");
			}
			string[] parts = cookie.Split(new char[] { ':' }, 3);
			if (parts.Length != 3)
			{
				throw new InvalidDataException("PX cookie format");
			}
			byte[] salt = Convert.FromBase64String(parts[0]);
			int iterations = int.Parse(parts[1]);
			if (iterations < 1 || iterations > 10000)
			{
				throw new ArgumentOutOfRangeException("iterations", "encryption iterations");
			}
			byte[] data = Convert.FromBase64String(parts[2]);

			using (RijndaelManaged AES = new RijndaelManaged())
			{
				var key = new byte[KEY_SIZE_BITS / 8];
				var iv = new byte[IV_SIZE_BITS / 8];
				var dk = PBKDF2Sha256GetBytes(key.Length + iv.Length, cookieKeyBytes, salt, iterations);
				Array.Copy(dk, key, key.Length);
				Array.Copy(dk, key.Length, iv, 0, iv.Length);

				AES.KeySize = KEY_SIZE_BITS;
				AES.BlockSize = IV_SIZE_BITS;
				AES.Key = key;
				AES.IV = iv;
				AES.Mode = CipherMode.CBC;
				var ms = new MemoryStream();
				using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
				{
					cs.Write(data, 0, data.Length);
				}
				var decryptedBytes = ms.ToArray();
				return Encoding.UTF8.GetString(decryptedBytes);
			}
		}

		private static byte[] PBKDF2Sha256GetBytes(int dklen, byte[] password, byte[] salt, int iterationCount)
		{
			using (var hmac = new HMACSHA256(password))
			{
				int hashLength = hmac.HashSize / 8;
				if ((hmac.HashSize & 7) != 0)
				{
					hashLength++;
				}
				int keyLength = dklen / hashLength;
				if (dklen > (0xFFFFFFFFL * hashLength) || dklen < 0)
				{
					throw new ArgumentOutOfRangeException("dklen");
				}
				if (dklen % hashLength != 0)
				{
					keyLength++;
				}
				byte[] extendedkey = new byte[salt.Length + 4];
				Buffer.BlockCopy(salt, 0, extendedkey, 0, salt.Length);
				using (var ms = new MemoryStream())
				{
					for (int i = 0; i < keyLength; i++)
					{
						extendedkey[salt.Length] = (byte)(((i + 1) >> 24) & 0xFF);
						extendedkey[salt.Length + 1] = (byte)(((i + 1) >> 16) & 0xFF);
						extendedkey[salt.Length + 2] = (byte)(((i + 1) >> 8) & 0xFF);
						extendedkey[salt.Length + 3] = (byte)(((i + 1)) & 0xFF);
						byte[] u = hmac.ComputeHash(extendedkey);
						Array.Clear(extendedkey, salt.Length, 4);
						byte[] f = u;
						for (int j = 1; j < iterationCount; j++)
						{
							u = hmac.ComputeHash(u);
							for (int k = 0; k < f.Length; k++)
							{
								f[k] ^= u[k];
							}
						}
						ms.Write(f, 0, f.Length);
						Array.Clear(u, 0, u.Length);
						Array.Clear(f, 0, f.Length);
					}
					byte[] dk = new byte[dklen];
					ms.Position = 0;
					ms.Read(dk, 0, dklen);
					ms.Position = 0;
					for (long i = 0; i < ms.Length; i++)
					{
						ms.WriteByte(0);
					}
					Array.Clear(extendedkey, 0, extendedkey.Length);
					return dk;
				}
			}
		}
	}
}
