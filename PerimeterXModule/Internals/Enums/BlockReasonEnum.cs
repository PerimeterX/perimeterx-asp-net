using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	[DataContract]
	public enum BlockReasonEnum
	{
		[EnumMember]
		NONE,
		[EnumMember(Value = "cookie_high_score")]
		COOKIE_HIGH_SCORE,
		[EnumMember(Value = "s2s_high_score")]
		RISK_HIGH_SCORE
	}
}
