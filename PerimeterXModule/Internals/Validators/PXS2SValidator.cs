using System;
using Jil;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace PerimeterX.Internals.Validators
{
    public class PXS2SValidator
    {

        private readonly PxModuleConfigurationSection PxConfig;
        private readonly PxContext PxContext;
        private readonly HttpClient HttpClient;

        public PXS2SValidator(PxModuleConfigurationSection PxConfig, PxContext PxContext, HttpClient HttpClient)
        {
            this.PxConfig = PxConfig;
            this.PxContext = PxContext;
            this.HttpClient = HttpClient;
        }

        public bool VerifyS2S(){
            try{
                RiskResponseV2 riskReponse = this.SendRiskResponse();
                PxContext.MadeS2SCallReason = true;

                if ( !double.IsNaN(riskReponse.Score) && !string.IsNullOrEmpty(riskReponse.RiskResponseAction))
                {
                    double score = riskReponse.Score;
                    PxContext.Score = score;
                    PxContext.UUID = riskReponse.Uuid;
                    PxContext.BlockAction = riskReponse.RiskResponseAction;

                    if (score >= PxConfig.BlockingScore)
                    {
                        PxContext.BlockReason = BlockReasonEnum.RISK_HIGH_SCORE;
                    }
                }

            }catch (Exception ex){
				Debug.WriteLine("Failed to verify S2S: " + ex.Message, PxConstants.LOG_CATEGORY);
				return false;  
            }
            return true;
        }

        public RiskResponseV2 SendRiskResponse(){

            var riskMode = ModuleMode.BLOCK_MODE;
            if (PxConfig.MonitorMode == ModuleMode.MONITOR_MODE)
            {
                riskMode = ModuleMode.MONITOR_MODE;
            }

            RiskRequestV2 riskRequest = new RiskRequestV2
            {
                Vid = PxContext.Vid,
                Request = RequestV2.CreateRequestFromContext(PxContext),
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

            if (PxContext.S2SCallReason.Equals(RiskRequestReasonEnum.DECRYPTION_FAILED)){
                riskRequest.Additional.PxOrigCookie = PxContext.getPxCookie();
            }else if (PxContext.S2SCallReason.Equals(RiskRequestReasonEnum.EXPIRED_COOKIE) || PxContext.S2SCallReason.Equals(RiskRequestReasonEnum.VALIDATION_FAILED)){
                riskRequest.Additional.PXCookie = this.PxContext.DecodedPxCookie;
            }

            var requestJson = JSON.Serialize(riskRequest, PxConstants.JSON_OPTIONS);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, PxConstants.RISK_API_V2)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
            };

			var httpResponse = HttpClient.SendAsync(requestMessage).Result;
			httpResponse.EnsureSuccessStatusCode();
			var responseJson = httpResponse.Content.ReadAsStringAsync().Result;
			Debug.WriteLine(string.Format("Post request for {0} ({1}), returned {2}", PxConstants.RISK_API_V2, requestJson, responseJson), PxConstants.LOG_CATEGORY);
            return JSON.Deserialize<RiskResponseV2>(responseJson, PxConstants.JSON_OPTIONS);
        }
    }
}
