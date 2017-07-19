using System;
using System.Net.Http;

namespace PerimeterX
{
	public interface PxClient
	{
		RiskResponse SendRiskRequest(RiskRequest riskRequest);
		CaptchaResponse SendCaptchaRequest(CaptchaRequest captchaRequest);
		PXDynamicConfiguration GetConfigurationRequest();
	}
}
