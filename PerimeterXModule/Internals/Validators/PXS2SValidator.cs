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

        private readonly PXConfigurationWrapper pxConfig;
        private readonly PxClient pxClient;

        public PXS2SValidator(PXConfigurationWrapper pxConfig, PxClient pxClient)
		{
			this.pxConfig = pxConfig;
			this.pxClient = pxClient;
		}

        public bool VerifyS2S(PxContext pxCtx)
		{
			var riskRttStart = Stopwatch.StartNew();
			bool retVal = false;
			try
			{
                RiskRequest riskRequest = PrepareRiskRequest(pxCtx);
                RiskResponse riskResponse = pxClient.SendRiskRequest(riskRequest);
				pxCtx.MadeS2SCallReason = true;

				if (riskResponse.Score >= 0 && !string.IsNullOrEmpty(riskResponse.RiskResponseAction))
				{
					int score = riskResponse.Score;
					pxCtx.Score = score;
					pxCtx.UUID = riskResponse.Uuid;
					pxCtx.BlockAction = riskResponse.RiskResponseAction;

					if (score >= pxConfig.BlockingScore)
					{
						pxCtx.BlockReason = BlockReasonEnum.RISK_HIGH_SCORE;
					}
					else
					{
						pxCtx.PassReason = PassReasonEnum.S2S;
					}
					retVal = true;
				}
				else
				{
					pxCtx.S2SHttpErrorMessage = riskResponse.ErrorMessage;
					retVal = false;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Failed to verify S2S: " + ex.Message, PxConstants.LOG_CATEGORY);
				pxCtx.PassReason = PassReasonEnum.ERROR;
				if (ex.InnerException is TaskCanceledException)
				{
                    Debug.WriteLine("S2S Inner Exception: " + ex.InnerException.Message, PxConstants.LOG_CATEGORY);
					pxCtx.PassReason = PassReasonEnum.S2S_TIMEOUT;
				}
				retVal = false;
			}
			pxCtx.RiskRoundtripTime = riskRttStart.ElapsedMilliseconds;
			riskRttStart.Stop();
			return retVal;
		}

        private RiskRequest PrepareRiskRequest(PxContext pxCtx)
		{

			var riskMode = ModuleMode.BLOCK_MODE;
			if (pxConfig.MonitorMode == true)
			{
				riskMode = ModuleMode.MONITOR_MODE;
			}

			RiskRequest riskRequest = new RiskRequest
			{
				Vid = pxCtx.Vid,
				Request = Request.CreateRequestFromContext(pxCtx),
				Additional = new Additional
				{
					CallReason = pxCtx.S2SCallReason,
					ModuleVersion = PxConstants.MODULE_VERSION,
					HttpMethod = pxCtx.HttpMethod,
					HttpVersion = pxCtx.HttpVersion,
					RiskMode = riskMode,
					PxCookieHMAC = pxCtx.PxCookieHmac

				}
			};

			if (!string.IsNullOrEmpty(pxCtx.Vid))
			{
				riskRequest.Vid = pxCtx.Vid;
			}


			if (!string.IsNullOrEmpty(pxCtx.UUID))
			{
				riskRequest.UUID = pxCtx.UUID;
			}

			if (pxCtx.S2SCallReason.Equals(RiskRequestReasonEnum.DECRYPTION_FAILED))
			{
				riskRequest.Additional.PxOrigCookie = pxCtx.getPxCookie();
			}
			else if (pxCtx.S2SCallReason.Equals(RiskRequestReasonEnum.EXPIRED_COOKIE) || pxCtx.S2SCallReason.Equals(RiskRequestReasonEnum.VALIDATION_FAILED))
			{
				riskRequest.Additional.PXCookie = pxCtx.DecodedPxCookie;
			}

            return riskRequest;
		}
	}
}
