using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public class RiskResponse
	{
		[DataMember(Name = "status")]
		public int Status;

		[DataMember(Name = "message")]
		public string Message;

		[DataMember(Name = "uuid")]
		public string Uuid;

		[DataMember(Name = "scores")]
		public RiskResponseScores Scores;
	}
}
