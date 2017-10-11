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
		private PxModuleConfigurationSection PxConfig;
		private HttpClient HttpClient;

		public PXCaptchaValidator(PxModuleConfigurationSection PxConfig, HttpClient HttpClient)
		{
			this.PxConfig = PxConfig;
			this.HttpClient = HttpClient;
		}

		public bool CaptchaVerify(PxContext context)
		{
			Debug.WriteLine(string.Format("Check captcha cookie {0} for {1}", context.PxCaptcha, context.Vid ?? ""), PxConstants.LOG_CATEGORY);
			bool retVal = false;
			var riskRttStart = Stopwatch.StartNew();
			try
			{
				var captchaAPIRequest = new CaptchaAPIRequest()
				{
					Hostname = context.Hostname,
					PXCaptcha = context.PxCaptcha,
					Request = CaptchaRequest.CreateCaptchaRequestFromContext(context, PxConfig.CaptchaProvider),
					Additional = new Additional { ModuleVersion = PxConstants.MODULE_VERSION }
				};
                
				var response = PostRequest(PxConstants.FormatBaseUri(PxConfig) + PxConstants.CAPTCHA_API_PATH, captchaAPIRequest);
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

		private CaptchaResponse PostRequest(string url, CaptchaAPIRequest request)
		{
			var requestJson = JSON.Serialize(request, PxConstants.JSON_OPTIONS);
			var requestMessage = new HttpRequestMessage(HttpMethod.Post, url);
			requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
			var httpResponse = this.HttpClient.SendAsync(requestMessage).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", url, requestJson, responseJson), PxConstants.LOG_CATEGORY);
			return JSON.Deserialize<CaptchaResponse>(responseJson, PxConstants.JSON_OPTIONS);
		}
	}
}
