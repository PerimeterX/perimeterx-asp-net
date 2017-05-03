using System.Runtime.Serialization;

namespace PerimeterX
{
    [DataContract]
    [System.Obsolete("Use RiskRequestV2")]
	public class RiskRequest
	{
		[DataMember(Name = "request")]
		public Request Request;

		[DataMember(Name = "vid", EmitDefaultValue = false)]
		public string Vid;

		[DataMember(Name = "additional", EmitDefaultValue = false)]
		public Additional Additional;
	}
}
