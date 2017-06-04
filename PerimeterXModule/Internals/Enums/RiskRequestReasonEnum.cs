using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	[DataContract]
	public enum RiskRequestReasonEnum
	{
		[EnumMember(Value = "none")]
		NONE,
		[EnumMember(Value = "no_cookie")]
		NO_COOKIE,
		[EnumMember(Value = "expired_cookie")]
		EXPIRED_COOKIE,
		[EnumMember(Value = "invalid_cookie")]
		INVALID_COOKIE,
		[EnumMember(Value = "cookie_decryption_failed")]
		DECRYPTION_FAILED,
		[EnumMember(Value = "cookie_validation_failed")]
		VALIDATION_FAILED,
		[EnumMember(Value = "sensitive_route")]
		SENSITIVE_ROUTE
	}
}
