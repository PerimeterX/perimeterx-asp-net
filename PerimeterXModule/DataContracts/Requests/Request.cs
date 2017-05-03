using System;
using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
    [System.Obsolete("Use RequestV2")]
	public class Request
	{
		[DataMember(Name = "ip")]
		public string IP;

		[DataMember(Name = "url")]
		public string URL;

		[DataMember(Name = "headers")]
		public RiskRequestHeader[] Headers;
	}
}
