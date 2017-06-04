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

		[DataMember(Name = "score")]
		public int Score;

		[DataMember(Name = "action")]
		public string RiskResponseAction;

		[DataMember(Name = "error_msg")]
		public string ErrorMessage;
	}
}
