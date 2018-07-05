using System.Runtime.Serialization;

namespace PerimeterX
{
	[DataContract]
	public enum PassReasonEnum
	{
		[EnumMember]
		NONE,
		[EnumMember(Value = "cookie")]
		COOKIE,
		[EnumMember(Value = "timeout")]
		TIMEOUTE,
		[EnumMember(Value = "s2s")]
		S2S,
		[EnumMember(Value = "s2s_timeout")]
		S2S_TIMEOUT,
		[EnumMember(Value = "error")]
		ERROR
	}
}
