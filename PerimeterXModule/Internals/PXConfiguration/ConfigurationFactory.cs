using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
	class ConfigurationFactory
	{
		public static IPXConfiguration Create(PxModuleConfigurationSection config)
		{
			IPXConfiguration pxConfig;
			if (config.RemoteConfigurationEnabled)
			{
				pxConfig = new PXDynamicConfiguration(config);
			}
			else
			{
				pxConfig = new PXConfiguration(config);
			}
			return pxConfig;
		}
	}
}
