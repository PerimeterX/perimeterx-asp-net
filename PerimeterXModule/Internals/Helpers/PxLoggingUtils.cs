using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


namespace PerimeterX
{

	class PxLoggingUtils
	{
		private static String m_appId;

		public static void init(String appId)
		{
			
			m_appId = appId;
		}

		public static void LogDebug(String message)
		{
			Debug.WriteLine("[PerimeterX - DEBUG] [{0}] - {1}", m_appId, message);
		}

		public static void LogError(String message)
		{
			Debug.WriteLine("[PerimeterX - ERROR] [{0}] - {1}", m_appId, message);
		}

	}
}
