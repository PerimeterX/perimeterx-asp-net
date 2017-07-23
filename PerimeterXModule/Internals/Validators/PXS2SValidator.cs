using Jil;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	public class PXS2SValidator : IPXS2SValidator
	{

		private readonly IPXConfiguration PxConfig;
		private readonly IPXHttpClient pxHttpClient;

		public PXS2SValidator(IPXConfiguration PxConfig, IPXHttpClient pxHttpClient)
		{
			this.PxConfig = PxConfig;
			this.pxHttpClient = pxHttpClient;
		}

		public bool VerifyS2S(PxContext PxContext)
		{
			var riskRttStart = Stopwatch.StartNew();
			bool retVal = false;
			try
			{
				RiskRequest riskRequest = PrepareRiskRequest(PxContext);
				string url = PxConstants.FormatBaseUri(PxConfig);
				string path = PxConstants.RISK_API_V2;
				RiskResponse riskResponse = pxHttpClient.SendRiskApi(url, path, riskRequest, PxConfig.ApiTimeout);
				PxContext.MadeS2SCallReason = true;

				if (riskResponse.Score >= 0 && !string.IsNullOrEmpty(riskResponse.RiskResponseAction))
				{
					int score = riskResponse.Score;
					PxContext.Score = score;
					PxContext.UUID = riskResponse.Uuid;
					PxContext.BlockAction = riskResponse.RiskResponseAction;

					if (score >= PxConfig.BlockingScore)
					{
						PxContext.BlockReason = BlockReasonEnum.RISK_HIGH_SCORE;
					}
					else
					{
						PxContext.PassReason = PassReasonEnum.S2S;
					}
					retVal = true;
				}
				else
				{
					PxContext.S2SHttpErrorMessage = riskResponse.ErrorMessage;
					retVal = false;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to verify S2S: " + ex.Message, PxConstants.LOG_CATEGORY);
				PxContext.PassReason = PassReasonEnum.ERROR;
				if (ex.InnerException is TaskCanceledException)
				{
					PxContext.PassReason = PassReasonEnum.S2S_TIMEOUT;
				}
				retVal = false;
			}
			PxContext.RiskRoundtripTime = riskRttStart.ElapsedMilliseconds;
			riskRttStart.Stop();
			return retVal;
		}

		private RiskRequest PrepareRiskRequest(PxContext PxContext)
		{
			var riskMode = ModuleMode.BLOCK_MODE;
			if (PxConfig.MonitorMode == true)
			{
				riskMode = ModuleMode.MONITOR_MODE;
			}

			RiskRequest riskRequest = new RiskRequest
			{
				Vid = PxContext.Vid,
				Request = Request.CreateRequestFromContext(PxContext),
				Additional = new Additional
				{
					CallReason = PxContext.S2SCallReason,
					ModuleVersion = PxConstants.MODULE_VERSION,
					HttpMethod = PxContext.HttpMethod,
					HttpVersion = PxContext.HttpVersion,
					RiskMode = riskMode,
					PxCookieHMAC = PxContext.PxCookieHmac

				}
			};

			if (!string.IsNullOrEmpty(PxContext.Vid))
			{
				riskRequest.Vid = PxContext.Vid;
			}


			if (!string.IsNullOrEmpty(PxContext.UUID))
			{
				riskRequest.UUID = PxContext.UUID;
			}

			if (PxContext.S2SCallReason.Equals(RiskRequestReasonEnum.DECRYPTION_FAILED))
			{
				riskRequest.Additional.PxOrigCookie = PxContext.getPxCookie();
			}
			else if (PxContext.S2SCallReason.Equals(RiskRequestReasonEnum.EXPIRED_COOKIE) || PxContext.S2SCallReason.Equals(RiskRequestReasonEnum.VALIDATION_FAILED))
			{
				riskRequest.Additional.PXCookie = PxContext.DecodedPxCookie;
			}

			return riskRequest;
		}
	}
}
