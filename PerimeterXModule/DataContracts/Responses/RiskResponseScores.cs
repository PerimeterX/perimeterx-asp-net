using System;
using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public class RiskResponseScores
	{
		[DataMember(Name = "filter")]
		public int Filter;

		[DataMember(Name = "suspected_script")]
		public int Application;

		[DataMember(Name = "non_human")]
		public int Bot;
	}
}
