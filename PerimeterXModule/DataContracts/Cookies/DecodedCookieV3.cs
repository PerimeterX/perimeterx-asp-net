using System.Runtime.Serialization;

namespace PerimeterX
{
	public class DecodedCookieV3
	{
		[DataMember(Name = "u")]
		public string Uuid { get; set; }
		[DataMember(Name = "v")]
		public string Vid { get; set; }
		[DataMember(Name = "t")]
		public double Time { get; set; }
		[DataMember(Name = "s")]
		public int Score;
		[DataMember(Name = "a")]
		public string Action;
	}
}
