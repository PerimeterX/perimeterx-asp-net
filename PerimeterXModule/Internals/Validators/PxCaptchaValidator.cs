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
				var response = PostRequest(PxConstants.FormatBaseUri(PxConfig) + PxConstants.CAPTCHA_API_V1, captchaRequest);
				if (response != null && response.Status == 0)
				{
					Debug.WriteLine("Captcha API call to server was successful", PxConstants.LOG_CATEGORY);
					context.PassReason = PassReasonEnum.CAPTCHA;
					context.RiskRoundtripTime = riskRttStart.ElapsedMilliseconds;
					return true;
				}
				Debug.WriteLine(string.Format("Captcha API call to server failed - {0}", response), PxConstants.LOG_CATEGORY);
				context.RiskRoundtripTime = riskRttStart.ElapsedMilliseconds;
				return false;
			}
			catch (Exception ex)
			{
				context.PassReason = PassReasonEnum.ERROR;
				context.RiskRoundtripTime = riskRttStart.ElapsedMilliseconds;
				if (ex.InnerException is TaskCanceledException)
				{
					context.PassReason = PassReasonEnum.CAPTCHA_TIMEOUT;
				}
				Debug.WriteLine(string.Format("Captcha API call to server failed with exception {0} - {1}", ex.Message, context.Uri), PxConstants.LOG_CATEGORY);
				// In any case of exception, request should pass
				return true;
			}

		}

		private CaptchaResponse PostRequest(string url, CaptchaRequest request)
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
