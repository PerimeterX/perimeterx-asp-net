using System;
namespace PerimeterX
{
	public interface RemoteConfigurationManager
	{
		PXDynamicConfiguration GetConfiguration();
		void UpdateConfiguration(PXDynamicConfiguration dynamicConfig);
		void DisableModuleOnError();
	}
}
