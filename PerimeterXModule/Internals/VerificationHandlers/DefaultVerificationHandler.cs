using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web;

namespace PerimeterX
{
    public class DefaultVerificationHandler : VerificationHandler
    {
        private IActivityReporter activityReporter;

        public DefaultVerificationHandler(IActivityReporter activityReporter)
        {
            this.activityReporter = activityReporter;
        }

        public bool HandleVerificatoin(PXConfigurationWrapper pxConfig, PxContext pxCtx, HttpApplication application)
        {
            int score = pxCtx.Score;

            if (score < pxConfig.BlockingScore || pxConfig.MonitorMode){
                Debug.WriteLine("Request was verified, passing request, score {0} mintor mode {1}", score, pxConfig.MonitorMode);
                PostPageRequestedActivity(pxCtx, pxConfig);
                return true;
            }

			Debug.WriteLine("Request was not verified, blocking request, score {0}", score, pxConfig.MonitorMode);
            PostBlockActivity(pxCtx, pxConfig);

            application.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            application.Response.TrySkipIisCustomErrors = true;
			//End request
			if (pxConfig.SuppressContentBlock)
			{
				pxCtx.ApplicationContext.Response.SuppressContent = true;
			}

            else
            {
                var html = ResponseBlockPage(pxCtx, pxConfig);
                application.Response.Write(html);
            }

            application.CompleteRequest();
            return false;
		}

		private void PostActivity(PxContext pxCtx, PXConfigurationWrapper pxConfig, string eventType, ActivityDetails details = null)
		{
			var activity = new Activity
			{
				Type = eventType,
				Timestamp = Math.Round(DateTime.UtcNow.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds, MidpointRounding.AwayFromZero),
				AppId = pxConfig.AppId,
				SocketIP = pxCtx.Ip,
				Url = pxCtx.FullUrl,
				Details = details
			};

			activity.Headers = new Dictionary<string, string>();

			foreach (RiskRequestHeader riskHeader in pxCtx.Headers)
			{
				var key = riskHeader.Name;
				activity.Headers.Add(key, riskHeader.Value);
			}

			activityReporter.Post(activity);
		}

        private void PostPageRequestedActivity(PxContext pxCtx, PXConfigurationWrapper pxConfig)
		{
			if (pxConfig.SendPageActivites)
			{
				PostActivity(pxCtx, pxConfig, "page_requested", new ActivityDetails
				{
					ModuleVersion = PxConstants.MODULE_VERSION,
					PassReason = pxCtx.PassReason,
					RiskRoundtripTime = pxCtx.RiskRoundtripTime,
					ClientUuid = pxCtx.UUID
				});
			}
		}

		private void PostBlockActivity(PxContext pxCtx, PXConfigurationWrapper pxConfig)
		{
			if (pxConfig.SendBlockActivites)
			{
				PostActivity(pxCtx, pxConfig, "block", new ActivityDetails
				{
					BlockReason = pxCtx.BlockReason,
					BlockUuid = pxCtx.UUID,
					ModuleVersion = PxConstants.MODULE_VERSION,
					RiskScore = pxCtx.Score,
					RiskRoundtripTime = pxCtx.RiskRoundtripTime
				});
			}
		}

        private string ResponseBlockPage(PxContext pxCtx, PXConfigurationWrapper pxConfig)
		{
			string template = "block";
			string content;
			if (pxCtx.BlockAction.Equals("c"))
			{
				template = "captcha";
			}
			Debug.WriteLine(string.Format("Using {0} template", template), PxConstants.LOG_CATEGORY);
			return content = TemplateFactory.getTemplate(template, pxConfig, pxCtx.UUID, pxCtx.Vid);
		}
    }
}
