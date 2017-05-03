using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
    class PXCookieValidator : IPXCookieValidator
    {

        public RiskRequestReasonEnum CookieVerify(PxContext context, IPxCookie pxCookie)
        {
            try
            {
                if (pxCookie == null)
                {
                    Debug.WriteLine("Request without risk cookie - " + context.Uri, PxConstants.LOG_CATEGORY);
                    return RiskRequestReasonEnum.NO_COOKIE;
                }

                // parse cookie and check if cookie valid
                pxCookie.Deserialize();

                context.DecodedPxCookie = pxCookie.GetDecodedCookie();
                context.Score = pxCookie.GetDecodedCookie().GetScore();
                context.UUID = pxCookie.GetDecodedCookie().GetUUID();
                context.Vid = pxCookie.GetDecodedCookie().GetVID();
                context.BlockAction = pxCookie.GetDecodedCookie().GetBlockAction();
                context.PxCookieHmac = pxCookie.GetDecodedCookieHMAC();

                if (pxCookie.IsExpired())
                {
                    Debug.WriteLine("Request with expired cookie - " + context.Uri, PxConstants.LOG_CATEGORY);
                    return RiskRequestReasonEnum.EXPIRED_COOKIE;
                }

                if (string.IsNullOrEmpty(pxCookie.GetDecodedCookieHMAC()))
                {
                    Debug.WriteLine("Request with invalid cookie (missing signature) - " + context.Uri, PxConstants.LOG_CATEGORY);
                    return RiskRequestReasonEnum.INVALID_COOKIE;
                }

                if (!pxCookie.IsSecured())
                {
                    Debug.WriteLine(string.Format("Request with invalid cookie (hash mismatch) {0}, {1}", pxCookie.GetDecodedCookieHMAC(), context.Uri), PxConstants.LOG_CATEGORY);
                    return RiskRequestReasonEnum.VALIDATION_FAILED;
                }

                return RiskRequestReasonEnum.NONE;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Request with invalid cookie (exception: " + ex.Message + ") - " + context.Uri, PxConstants.LOG_CATEGORY);
            }
            return RiskRequestReasonEnum.DECRYPTION_FAILED;
        }
    }
}
