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

		[DataMember(Name = "uri")]
		public string URI { get; set; }

		public static Request CreateRequestFromContext(PxContext pxContext)
		{
			return new Request
			{
				IP = pxContext.Ip,
				URL = pxContext.FullUrl,
				URI = pxContext.Uri,
				Headers = pxContext.Headers.ToArray()
			};
		}
	}

    [DataContract]
    [Serializable]
    public class CaptchaRequest : Request
    {
        [DataMember(Name = "captchaType")]
        public string CaptchaType { get; set; }

        public static CaptchaRequest CreateCaptchaRequestFromContext(PxContext pxContext, string captchaType)
		{
			return new CaptchaRequest
			{
				IP = pxContext.Ip,
				URL = pxContext.FullUrl,
				URI = pxContext.Uri,
				Headers = pxContext.Headers.ToArray(),
                CaptchaType = captchaType
			};
		}
    }
}