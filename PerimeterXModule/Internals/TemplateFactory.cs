using Nustache.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PerimeterX
{
	abstract class TemplateFactory
	{

		public static string getTemplate(string template, PxModuleConfigurationSection pxConfiguration, string uuid, string vid)
		{
			string templateStr = getTemplateString(template);
			return Render.StringToString(templateStr, getProps(pxConfiguration, uuid, vid));

		}

		private static string getTemplateString(string template)
		{
			string templateStr = "";
			Assembly _assembly = Assembly.GetExecutingAssembly();
			StreamReader _textStream = new StreamReader(_assembly.GetManifestResourceStream(string.Format("PerimeterX.Internals.Templates.{0}.mustache", template)));

			while (_textStream.Peek() != -1)
			{
				templateStr = string.Concat(templateStr, _textStream.ReadLine());
			}
			if (string.IsNullOrEmpty(templateStr))
			{
				throw new Exception(string.Format("Unable to read template {0} from asm", template));
			}

			return templateStr;
		}

		private static IDictionary<String, String> getProps(PxModuleConfigurationSection pxConfiguration, string uuid, string vid)
		{
			IDictionary<String, String> props = new Dictionary<String, String>();
			props.Add("refId", uuid);
			props.Add("appId", pxConfiguration.AppId);
			props.Add("vid", vid);
			props.Add("uuid", uuid);
			props.Add("customLogo", pxConfiguration.CustomLogo);
			props.Add("cssRef", pxConfiguration.CssRef);
			props.Add("jsRef", pxConfiguration.JsRef);
			props.Add("logoVisibility", string.IsNullOrEmpty(pxConfiguration.CustomLogo) ? "hidden" : "visible");

			return props;
		}
	}
}
