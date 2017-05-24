﻿using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public class ActivityDetails
	{
		[DataMember(Name = "block_reason", EmitDefaultValue = true)]
		public BlockReasonEnum? BlockReason;

		[DataMember(Name = "block_uuid")]
		public string BlockUuid;

		[DataMember(Name = "module_version")]
		public string ModuleVersion;

		[DataMember(Name = "block_score")]
		public double RiskScore;

		[DataMember(Name = "pass_reason", EmitDefaultValue = true)]
		public PassReasonEnum? PassReason;

		[DataMember(Name = "risk_rtt")]
		public double RiskRoundtripTime;
	}
}