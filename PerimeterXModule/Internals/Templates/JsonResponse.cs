using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	class JsonResponse : IJsonResponse
	{
		[DataMember(Name = "appId")]
		public string AppId;
		[DataMember(Name = "jsClientSrc")]
		public string JsClientSrc;
		[DataMember(Name = "uuid")]
		public string Uuid;
		[DataMember(Name = "vid")]
		public string Vid;
		[DataMember(Name = "hostUrl")]
		public string HostUrl;
		[DataMember(Name = "blockScript")]
		public string BlockScript;
        [DataMember(Name = "firstPartyEnabled")]
        public string firstPartyEnabled;
    }
}
