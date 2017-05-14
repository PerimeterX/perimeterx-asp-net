using System;
using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	[Serializable]
	public class RequestV2 : Request
	{
		[DataMember(Name = "uri")]
		public string URI { get; set; }

		public static new RequestV2 CreateRequestFromContext(PxContext pxContext)
		{
			return new RequestV2
			{
				IP = pxContext.Ip,
				URL = pxContext.FullUrl,
				URI = pxContext.Uri,
				Headers = pxContext.Headers.ToArray()
			};
		}
	}
}
