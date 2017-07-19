using Jil;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{

	class PXCaptchaValidator : IPXCaptchaValidator
	{
		private PXConfigurationWrapper pxConfig;
		private PxClient pxClient;

		public PXCaptchaValidator(PXConfigurationWrapper PxConfig, PxClient pxClient)
		{
			this.pxConfig = PxConfig;
			this.pxClient = pxClient;
		}


		public bool CaptchaVerify(PxContext context)
		{
			Debug.WriteLine(string.Format("Check captcha cookie {0} for {1}", context.PxCaptcha, context.Vid ?? ""), PxConstants.LOG_CATEGORY);
			bool retVal = false;
			var riskRttStart = Stopwatch.StartNew();
			try
			{
				var captchaRequest = new CaptchaRequest()
				{
					Hostname = context.Hostname,
					PXCaptcha = context.PxCaptcha,
					Vid = context.Vid,
					Request = Request.CreateRequestFromContext(context)
				};
				var response = pxClient.SendCaptchaRequest(captchaRequest);
				if (response != null && response.Status == 0)
				{
					Debug.WriteLine("Captcha API call to server was successful", PxConstants.LOG_CATEGORY);
					context.PassReason = PassReasonEnum.CAPTCHA;
					retVal = true;
				}
				else
				{
					Debug.WriteLine(string.Format("Captcha API call to server failed - {0}", response), PxConstants.LOG_CATEGORY);
					retVal = false;
				}

			}
			catch (Exception ex)
			{
				context.PassReason = PassReasonEnum.ERROR;
				if (ex.InnerException is TaskCanceledException)
				{
					context.PassReason = PassReasonEnum.CAPTCHA_TIMEOUT;
				}
				Debug.WriteLine(string.Format("Captcha API call to server failed with exception {0} - {1}", ex.Message, context.Uri), PxConstants.LOG_CATEGORY);
				// In any case of exception, request should pass
				retVal = true;
			}
			context.RiskRoundtripTime = riskRttStart.ElapsedMilliseconds;
			riskRttStart.Stop();
			return retVal;

		}


	}
}
