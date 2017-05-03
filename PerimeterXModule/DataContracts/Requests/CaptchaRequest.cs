using System;
using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public class CaptchaRequest
	{
		[DataMember(Name = "request")]
		public Request Request;

		[DataMember(Name = "vid", EmitDefaultValue = false)]
		public string Vid;

		[DataMember(Name = "pxCaptcha")]
		public string PXCaptcha;

		[DataMember(Name = "hostname")]
		public string Hostname;
	}
}
