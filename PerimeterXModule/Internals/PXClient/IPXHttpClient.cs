﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	public interface IPXHttpClient
	{
		RiskResponse SendRiskApi(string url, RiskRequest riskRequest, int timeout);
		CaptchaResponse SendCaptchaApi(string url, CaptchaRequest request, int timeout);
		RemoteConfigurationResponse GetConfiguration(string url, string path, string checksum);
	}
}
