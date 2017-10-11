using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	[DataContract]
	public class RemoteConfigurationResponse
	{
		[DataMember(Name = "moduleEnabled")]
		public bool ModuleEnabled { get; set; }
		[DataMember(Name = "cookieKey")]
		public string CookieKey { get; set; }
		[DataMember(Name = "blockingScore")]
		public int BlockingScore { get; set; }
		[DataMember(Name = "appId")]
		public string AppId { get; set; }
		[DataMember(Name = "moduleMode")]
		public string ModuleMode { get; set; }
		[DataMember(Name = "ipHeaders")]
		public string[] IpHeaders { get; set; }
		[DataMember(Name = "sensitiveHeaders")]
		public string[] SensitiveHeaders { get; set; }
		[DataMember(Name = "connectTimeout")]
		public int ConnectTimeout { get; set; }
		[DataMember(Name = "riskTimeout")]
		public int RiskTimeout { get; set; }
		[DataMember(Name = "debugMode")]
		public Boolean DebugMode { get; set; }
		[DataMember(Name = "checksum")]
		public string Checksum { get; set; }
	}
}
