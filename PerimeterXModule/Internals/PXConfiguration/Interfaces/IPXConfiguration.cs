using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	public interface IPXConfiguration
	{
		bool Enabled { get; }
		string AppId { get; }
		string CookieName { get; }
		string CookieKey { get; }
		bool EncryptionEnabled { get; }
		bool CaptchaEnabled { get; }
		bool SignedWithUserAgent { get; }
		bool SignedWithIP { get; }
		int BlockingScore { get; }
		string ApiToken { get; }
		string BaseUri { get; }
		int ApiTimeout { get; }
		int ReporterApiTimeout { get; }
		StringCollection SocketIpHeaders { get; }
		bool SuppressContentBlock { get; }
		int ActivitiesCapacity { get; }
		int ActivitiesBulkSize { get; }
		bool SendPageActivites { get; }
		bool SendBlockActivites { get; }
		StringCollection SensitiveHeaders { get; }
		StringCollection FileExtWhitelist { get; }
		StringCollection RoutesWhitelist { get; }
		StringCollection UseragentsWhitelist { get; }
		string CustomLogo { get; }
		string CssRef { get; }
		string JsRef { get; }
		string UserAgentOverride { get; }
		bool MonitorMode { get; }
		StringCollection SensitiveRoutes { get; }
		bool RemoteConfigurationEnabled { get; }
		string RemoteConfigurationUrl { get; }
		int RemoteConfigurationInterval { get; }
	}
}
