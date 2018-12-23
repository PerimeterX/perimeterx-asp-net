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

		[DataMember(Name = "action_data")]
		public ActionData RiskResponseActionData;

		[DataMember(Name = "error_msg")]
		public string ErrorMessage;

		[DataMember(Name = "data_enrichment")]
		public object DataEnrichment;
	}


	[DataContract]
	public class ActionData
	{
		[DataMember(Name = "body")]
		public string Body;
	}
}
