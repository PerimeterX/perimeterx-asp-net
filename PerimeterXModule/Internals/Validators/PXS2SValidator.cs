using Jil;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PerimeterX
{
    public class PXS2SValidator : IPXS2SValidator
    {

        private readonly PxModuleConfigurationSection PxConfig;
        private readonly HttpHandler httpHandler;

        public PXS2SValidator(PxModuleConfigurationSection PxConfig, HttpHandler httpHandler)
        {
            this.PxConfig = PxConfig;
            this.httpHandler = httpHandler;
        }

        public bool VerifyS2S(PxContext PxContext)
        {
            var riskRttStart = Stopwatch.StartNew();
            bool retVal = false;
            try
            {
                RiskResponse riskResponse = SendRiskResponse(PxContext);
                PxContext.MadeS2SCallReason = true;

                if (riskResponse.Score >= 0 && !string.IsNullOrEmpty(riskResponse.RiskResponseAction))
                {
                    int score = riskResponse.Score;
                    PxContext.Score = score;
                    PxContext.UUID = riskResponse.Uuid;
                    PxContext.BlockAction = riskResponse.RiskResponseAction;

                    if (PxContext.BlockAction == PxConstants.JS_CHALLENGE_ACTION &&
                        !string.IsNullOrEmpty(riskResponse.RiskResponseActionData.Body))
                    {
                        PxContext.BlockReason = BlockReasonEnum.CHALLENGE;
                        PxContext.BlockData = riskResponse.RiskResponseActionData.Body;
                    }
                    else if (score >= PxConfig.BlockingScore)
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

        public RiskResponse SendRiskResponse(PxContext PxContext)
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
                riskRequest.Additional.PxOrigCookie = PxContext.GetPxCookie();
            }
            else if (PxContext.S2SCallReason.Equals(RiskRequestReasonEnum.COOKIE_EXPIRED) || PxContext.S2SCallReason.Equals(RiskRequestReasonEnum.VALIDATION_FAILED))
            {
                riskRequest.Additional.PXCookie = PxContext.DecodedPxCookie;
            }

            string requestJson = JSON.SerializeDynamic(riskRequest, PxConstants.JSON_OPTIONS);
            var responseJson = httpHandler.Post(requestJson, PxConstants.RISK_API_V2);
            return JSON.Deserialize<RiskResponse>(responseJson, PxConstants.JSON_OPTIONS);
        }
    }
}
