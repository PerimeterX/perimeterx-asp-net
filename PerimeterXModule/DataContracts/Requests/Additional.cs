using System;
using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public class Additional
	{
		[DataMember(Name = "px_cookie", EmitDefaultValue = false)]
		public string PXCookie;

		[DataMember(Name = "http_method")]
		public string HttpMethod;

		[DataMember(Name = "http_version")]
		public string HttpVersion;

		[DataMember(Name = "module_version")]
		public string ModuleVersion { get { return PxModule.MODULE_VERSION; } set { } }

		[DataMember(Name = "s2s_call_reason")]
		public RiskRequestReasonEnum CallReason;

		[DataMember(Name = "px_orig_cookie")]
		public string PxOrigCookie;
	}
}
