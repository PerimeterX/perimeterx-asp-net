using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{

	[DataContract]
	public enum ModuleMode
	{
		[EnumMember(Value = "monitor_mode")]
		MONITOR_MODE = 1,

		[EnumMember(Value = "block_mode")]
		BLOCK_MODE = 0
	}
}
