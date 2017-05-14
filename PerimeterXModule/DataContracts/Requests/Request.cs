using System;
using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	[Serializable]
	public class Request
	{
		[DataMember(Name = "ip")]
		public string IP { get; set; }

		[DataMember(Name = "url")]
		public string URL { get; set; }

		[DataMember(Name = "headers")]
		public RiskRequestHeader[] Headers { get; set; }

		public static Request CreateRequestFromContext(PxContext pxContext)
		{
			return new Request
			{
				IP = pxContext.Ip,
				URL = pxContext.FullUrl,
				Headers = pxContext.Headers.ToArray()
			};
		}
	}
}
