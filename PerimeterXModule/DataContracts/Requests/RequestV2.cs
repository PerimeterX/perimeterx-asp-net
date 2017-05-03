using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public class RequestV2
	{
		[DataMember(Name = "ip")]
		public string IP;

		[DataMember(Name = "url")]
		public string URL;

		[DataMember(Name = "uri")]
		public string URI;

		[DataMember(Name = "headers")]
		public RiskRequestHeader[] Headers;

        public static RequestV2 CreateRequestFromContext(PxContext pxContext){
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
