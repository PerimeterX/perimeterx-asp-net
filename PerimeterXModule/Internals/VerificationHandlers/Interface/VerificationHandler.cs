using System;
using System.Web;

namespace PerimeterX
{
	public interface VerificationHandler
	{
		bool HandleVerificatoin(PXConfigurationWrapper pxConfig, PxContext pxCtx, HttpApplication application);
	}
}