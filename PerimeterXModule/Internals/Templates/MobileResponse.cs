using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	[DataContract]
	class MobileResponse
	{
		[DataMember(Name = "appId")]
		public string AppId;
		[DataMember(Name = "action")]
		public string Action;
		[DataMember(Name = "uuid")]
		public string Uuid;
		[DataMember(Name = "vid")]
		public string Vid;
		[DataMember(Name = "page")]
		public string Page;
		[DataMember(Name = "collectorUrl")]
		public string CollectorUrl;
	}
}
