﻿using System;
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
		public IPXCookieValidator PXOriginalTokenValidator { get; set; }

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
					PxLoggingUtils.LogDebug("Request without risk cookie - " + context.Uri);
					context.S2SCallReason = CALL_REASON_NO_COOKIE;
					return false;
				}

				if (context.IsMobileRequest)
				{
					string authorizatoinHeader = context.MobileHeader;
					if (!string.IsNullOrEmpty(authorizatoinHeader) && Regex.Match(authorizatoinHeader, MOBILE_PATTERN_ERROR).Success)
					{
						context.S2SCallReason = string.Format(CALL_REASON_MOBILE_ERROR, authorizatoinHeader);
						// Process original token
						if (context.OriginalToken != null)
						{
							try
							{
								PxLoggingUtils.LogDebug(string.Format("Found original token in context"));
								PXOriginalTokenValidator.Verify(context, context.OriginalToken);
							}
							catch (Exception e)
							{
								PxLoggingUtils.LogDebug(string.Format("Failed to verify original token: {0}", e.Message));
								context.OriginalTokenError = CALL_REASON_DECRYPTION_FAILED;
							}
						}
						PxLoggingUtils.LogDebug(string.Format("Mobile sdk error found {0}", context.S2SCallReason));
						return false;
					}
				}

				// parse cookie and check if cookie valid
				if (!pxCookie.Deserialize())
				{
					PxLoggingUtils.LogDebug("Cookie decryption failed" + context.Uri);
					context.S2SCallReason = CALL_REASON_DECRYPTION_FAILED;
					return false;
				}

				context.DecodedPxCookie = pxCookie.DecodedCookie;
				context.Score = pxCookie.Score;
				context.UUID = pxCookie.Uuid;
				context.Vid = pxCookie.Vid;
				context.VidSource = PxConstants.RISK_COOKIE;
				context.BlockAction = pxCookie.BlockAction;
				context.PxCookieHmac = pxCookie.Hmac;

				if (PxCookieUtils.IsExpired(pxCookie.Timestamp))
				{
					PxLoggingUtils.LogDebug("Request with expired cookie - " + context.Uri);
					context.S2SCallReason = CALL_REASON_EXPIRED_COOKIE;
					return false;
				}

				if (pxCookie.Score >= config.BlockingScore)
				{
					context.BlockReason = BlockReasonEnum.COOKIE_HIGH_SCORE;
					PxLoggingUtils.LogDebug(string.Format("Request blocked by risk cookie UUID {0}, VID {1}", pxCookie.Uuid, pxCookie.Vid));
					return true;
				}

				if (string.IsNullOrEmpty(pxCookie.Hmac))
				{
					PxLoggingUtils.LogDebug("Request with invalid cookie (missing signature) - " + context.Uri);
					context.S2SCallReason = CALL_REASON_VALIDATION_FAILED;
					return false;
				}

				if (!pxCookie.IsSecured(config.CookieKey, getAdditionalSignedFields(context)))
				{
					PxLoggingUtils.LogDebug(string.Format("Request with invalid cookie (hash mismatch) {0}, {1}", pxCookie.Hmac, context.Uri));
					context.S2SCallReason = CALL_REASON_VALIDATION_FAILED;
					return false;
				}

				if (context.SensitiveRoute || context.LoginCredentialsFields != null)
				{
					PxLoggingUtils.LogDebug(string.Format("Cookie is valid but is a sensitive route {0}", context.Uri));
					context.S2SCallReason = CALL_REASON_SENSITIVE_ROUTE;
					return false;
				}

				context.PassReason = PassReasonEnum.COOKIE;
				return true;
			}
			catch (Exception ex)
			{
				PxLoggingUtils.LogDebug("Request with invalid cookie (exception: " + ex.Message + ") - " + context.Uri);
				context.S2SCallReason = CALL_REASON_DECRYPTION_FAILED;
				return false;
			}
		}

		protected string[] getAdditionalSignedFields(PxContext context)
		{
			// Dont sign anything if cookie is mobile
			if (context.IsMobileRequest)
			{
				return new string[] { };
			}

			return config.SignedWithIP ?
				new string[] { context.Ip, context.UserAgent } :
				new string[] { context.UserAgent };
		}
	}
}
