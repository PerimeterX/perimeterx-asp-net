using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	[DataContract]
	public enum CookieOrigin
	{
		[EnumMember(Value = "header")]
		HEADER,
		[EnumMember(Value = "cookie")]
		COOKIE
	}
}
