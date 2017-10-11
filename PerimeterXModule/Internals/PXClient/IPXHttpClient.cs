using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	public interface IPXHttpClient : IDisposable
	{
		RiskResponse SendRiskApi(string url, string path, RiskRequest riskRequest, int timeout);
		CaptchaResponse SendCaptchaApi(string url, string path, CaptchaRequest request, int timeout);
		RemoteConfigurationResponse GetConfiguration(string url, string path, string checksum);
	}
}
