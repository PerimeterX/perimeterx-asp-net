using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PerimeterX
{
    [DataContract]
	public class Activity
	{
		[DataMember(Name = "type")]
		public string Type;

		[DataMember(Name = "url")]
		public string Url;

		[DataMember(Name = "px_app_id")]
		public string AppId;

		[DataMember(Name = "vid", EmitDefaultValue = false)]
		public string Vid;

		[DataMember(Name = "timestamp")]
		public double Timestamp;

		[DataMember(Name = "socket_ip", EmitDefaultValue = false)]
		public string SocketIP;

		[DataMember(Name = "headers", EmitDefaultValue = false)]
		public Dictionary<string, string> Headers;

		[DataMember(Name = "details", EmitDefaultValue = false)]
		public ActivityDetails Details;
	}

}
