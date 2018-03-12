using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Configuration;
using PerimeterX;

namespace PerimeterXModuleTest
{
	[TestClass]
	public class RemoteConfigurationTest
	{

		PxModuleConfigurationSection pxConfigSection;

		[TestInitialize]
		public void Init()
		{
			pxConfigSection = new PxModuleConfigurationSection()
			{
				AppId = "PX500TEST",
				CookieKey = "cOoKieToKeNtEsT",
				ApiToken = "aPiToKeN"
			};
		}

		[TestMethod]
		public void CreateNotDynamicRemoteConfiguration()
		{
			IPXConfiguration pxConfiguration = ConfigurationFactory.Create(pxConfigSection);
			Assert.IsTrue(pxConfiguration.GetType() == typeof(PxModuleConfigurationSection));
		}


		[TestMethod]
		public void CreatDynamicRemoteConfiguration()
		{
			pxConfigSection.RemoteConfigurationEnabled = true;
			IPXConfiguration pxConfiguration = ConfigurationFactory.Create(pxConfigSection);
			Assert.IsTrue(pxConfiguration.GetType() == typeof(PXDynamicConfiguration));

			((PXDynamicConfiguration)pxConfiguration).Dispose();
		}


	
	}
}
