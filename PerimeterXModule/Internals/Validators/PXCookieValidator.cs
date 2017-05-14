using System;
using System.Diagnostics;

namespace PerimeterX
{
	class PXCookieValidator : IPXCookieValidator
	{
		private PxModuleConfigurationSection config;

		public PXCookieValidator(PxModuleConfigurationSection config)
		{
			this.config = config;
		}

		public bool CookieVerify(PxContext context, IPxCookie pxCookie)
		{
			try
			{
				if (pxCookie == null)
				{
					Debug.WriteLine("Request without risk cookie - " + context.Uri, PxConstants.LOG_CATEGORY);
					context.S2SCallReason = RiskRequestReasonEnum.NO_COOKIE;
					return false;
				}

				// parse cookie and check if cookie valid
				if (!pxCookie.Deserialize())
				{
					Debug.WriteLine("Cookie decryption failed" + context.Uri, PxConstants.LOG_CATEGORY);
					context.S2SCallReason = RiskRequestReasonEnum.DECRYPTION_FAILED;
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
					context.S2SCallReason = RiskRequestReasonEnum.EXPIRED_COOKIE;
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
					context.S2SCallReason = RiskRequestReasonEnum.INVALID_COOKIE;
					return false;
				}

				if (!pxCookie.IsSecured(config.CookieKey, getAdditionalSignedFields(context)))
				{
					Debug.WriteLine(string.Format("Request with invalid cookie (hash mismatch) {0}, {1}", pxCookie.Hmac, context.Uri), PxConstants.LOG_CATEGORY);
					context.S2SCallReason = RiskRequestReasonEnum.VALIDATION_FAILED;
					return false;
				}
				context.S2SCallReason = RiskRequestReasonEnum.NONE;
				return true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Request with invalid cookie (exception: " + ex.Message + ") - " + context.Uri, PxConstants.LOG_CATEGORY);
				context.S2SCallReason = RiskRequestReasonEnum.DECRYPTION_FAILED;
				return false;
			}
		}

		private string[] getAdditionalSignedFields(PxContext context)
		{
			return config.SignedWithIP ?
				new string[] { context.Ip, context.UserAgent } :
				new string[] { context.UserAgent };
		}
	}
}
