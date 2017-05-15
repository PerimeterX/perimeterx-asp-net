using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public class RiskRequestHeader
	{
		[DataMember(Name = "name")]
		public string Name;

		[DataMember(Name = "value")]
		public string Value;
	}
}
