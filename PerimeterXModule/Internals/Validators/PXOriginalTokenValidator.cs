using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	public class PXOriginalTokenValidator : PXCookieValidator
	{
		public PXOriginalTokenValidator(PxModuleConfigurationSection config) : base(config)
		{
		}

		public override bool Verify(PxContext context, IPxCookie pxCookie)
		{
			if (pxCookie == null)
			{
				return false;
			}

			if (!pxCookie.Deserialize())
			{
				Debug.WriteLine("Failed to decode original token", PxConstants.LOG_CATEGORY);
				context.OriginalTokenError = CALL_REASON_DECRYPTION_FAILED;
				return false;
			}

			context.DecodedOriginalToken = pxCookie.DecodedCookie;
			context.OriginalUUID = pxCookie.Uuid;
			context.Vid = pxCookie.Vid;

			if (!pxCookie.IsSecured(config.CookieKey, getAdditionalSignedFields(context)))
			{
				Debug.WriteLine("Cookie HMAC validation failed", PxConstants.LOG_CATEGORY);
				context.OriginalTokenError = CALL_REASON_VALIDATION_FAILED;
				return false;
			}

			Debug.WriteLine("Original token verification passed succesfully", PxConstants.LOG_CATEGORY);

			return true;
		}
	}
}
