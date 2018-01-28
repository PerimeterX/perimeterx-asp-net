using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PerimeterX
{
	public class PXCookieValidator : IPXCookieValidator
	{
		public static readonly string CALL_REASON_INVALID_COOKIE = "invalid_cookie";
		public static readonly string CALL_REASON_DECRYPTION_FAILED = "cookie_decryption_failed";
		public static readonly string CALL_REASON_VALIDATION_FAILED = "cookie_validation_failed";
		public static string CALL_REASON_NO_COOKIE = "no_cookie";
		public static string CALL_REASON_EXPIRED_COOKIE = "cookie_expired";
		public static string CALL_REASON_SENSITIVE_ROUTE = "sensitive_route";
		public static string CALL_REASON_MOBILE_ERROR = "mobile_error_{0}";
		public IPXCookieValidator PXOriginalCookieValidator { get; set; }

		protected PxModuleConfigurationSection config;
		private readonly string MOBILE_PATTERN_ERROR = @"^\d+$";

		public PXCookieValidator(PxModuleConfigurationSection config)
		{
			this.config = config;
		}

		public virtual bool Verify(PxContext context, IPxCookie pxCookie)
		{
			try
			{
				if (pxCookie == null)
				{
					Debug.WriteLine("Request without risk cookie - " + context.Uri, PxConstants.LOG_CATEGORY);
					context.S2SCallReason = CALL_REASON_NO_COOKIE;
					return false;
				}

				if (context.CookieOrigin.Equals(CookieOrigin.HEADER))
				{
					string authorizatoinHeader = context.GetHeadersAsDictionary()[PxConstants.MOBILE_HEADER.ToLower()];
					Regex match = new Regex(MOBILE_PATTERN_ERROR);
					if (!string.IsNullOrEmpty(authorizatoinHeader) && match.IsMatch(authorizatoinHeader))
					{
						context.S2SCallReason = string.Format(CALL_REASON_MOBILE_ERROR, authorizatoinHeader);
						// Process original token
						if (context.OriginalToken != null)
						{
							try
							{
								Debug.WriteLine(string.Format("Found original token in context"), PxConstants.LOG_CATEGORY);
								PXOriginalCookieValidator.Verify(context, context.OriginalToken);
							}
							catch (Exception e)
							{
								Debug.WriteLine(string.Format("Failed to verify original token: {0}", e.Message) , PxConstants.LOG_CATEGORY);
								context.OriginalTokenError = CALL_REASON_DECRYPTION_FAILED;
							}
						}
						return false;
					}
				}

				// parse cookie and check if cookie valid
				if (!pxCookie.Deserialize())
				{
					Debug.WriteLine("Cookie decryption failed" + context.Uri, PxConstants.LOG_CATEGORY);
					context.S2SCallReason = CALL_REASON_DECRYPTION_FAILED;
					return false;
				}

				context.DecodedPxCookie = pxCookie.DecodedCookie;
				context.Score = pxCookie.Score;
				context.UUID = pxCookie.Uuid;
				context.Vid = pxCookie.Vid;
				context.BlockAction = pxCookie.BlockAction;
				context.PxCookieHmac = pxCookie.Hmac;

				if (PxCookieUtils.IsExpired(pxCookie.Timestamp))
				{
					Debug.WriteLine("Request with expired cookie - " + context.Uri, PxConstants.LOG_CATEGORY);
					context.S2SCallReason = CALL_REASON_EXPIRED_COOKIE;
					return false;
				}

				if (pxCookie.Score >= config.BlockingScore)
				{
					context.BlockReason = BlockReasonEnum.COOKIE_HIGH_SCORE;
					Debug.WriteLine(string.Format("Request blocked by risk cookie UUID {0}, VID {1}", pxCookie.Uuid, pxCookie.Vid), PxConstants.LOG_CATEGORY);
					return true;
				}

				if (string.IsNullOrEmpty(pxCookie.Hmac))
				{
					Debug.WriteLine("Request with invalid cookie (missing signature) - " + context.Uri, PxConstants.LOG_CATEGORY);
					context.S2SCallReason = CALL_REASON_VALIDATION_FAILED;
					return false;
				}

				if (!pxCookie.IsSecured(config.CookieKey, getAdditionalSignedFields(context)))
				{
					Debug.WriteLine(string.Format("Request with invalid cookie (hash mismatch) {0}, {1}", pxCookie.Hmac, context.Uri), PxConstants.LOG_CATEGORY);
					context.S2SCallReason = CALL_REASON_VALIDATION_FAILED;
					return false;
				}

				if (context.SensitiveRoute)
				{
					Debug.WriteLine(string.Format("Cookie is valid but is a sensitive route {0}", context.Uri), PxConstants.LOG_CATEGORY);
					context.S2SCallReason = CALL_REASON_SENSITIVE_ROUTE;
					return false;
				}

				context.PassReason = PassReasonEnum.COOKIE;
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Request with invalid cookie (exception: " + ex.Message + ") - " + context.Uri, PxConstants.LOG_CATEGORY);
				context.S2SCallReason = CALL_REASON_DECRYPTION_FAILED;
				return false;
			}
		}

		protected string[] getAdditionalSignedFields(PxContext context)
		{
			// Dont sign anything if cookie is mobile
			if (context.CookieOrigin.Equals(CookieOrigin.COOKIE))
			{
				return new string[] { };
			}

			return config.SignedWithIP ?
				new string[] { context.Ip, context.UserAgent } :
				new string[] { context.UserAgent };
		}
	}
}
