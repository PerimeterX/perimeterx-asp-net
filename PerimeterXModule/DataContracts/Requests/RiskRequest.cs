﻿using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public class RiskRequest
	{
		[DataMember(Name = "request")]
		public Request Request;

		[DataMember(Name = "vid", EmitDefaultValue = false)]
		public string Vid;

		[DataMember(Name = "uuid", EmitDefaultValue = false)]
		public string UUID;

		[DataMember(Name = "additional", EmitDefaultValue = false)]
		public Additional Additional;
	}
}