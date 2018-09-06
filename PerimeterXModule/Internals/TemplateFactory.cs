using Nustache.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PerimeterX
{
	abstract class TemplateFactory
	{
		private static readonly string CLIENT_SRC_FP = "/{0}/init.js";
		private static readonly string CLIENT_SRC_TP = "{0}/{1}/main.min.js";
		private static readonly string CAPTCHA_QUERY_PARAMS = "?a={0}&u={1}&v={2}&m={3}";
		private static readonly string CAPTCHA_SRC_FP = "/{0}/captcha/captcha.js{1}";
		private static readonly string CAPTCHA_SRC_TP = "{0}/{1}/captcha.js{2}";
		private static readonly string HOST_FP = "/{0}/xhr";


		public static string getTemplate(string template, PxModuleConfigurationSection pxConfiguration, string uuid, string vid, bool isMobileRequest,string action)
		{
			PxLoggingUtils.LogDebug(string.Format("Using {0} template", template));
			string templateStr = getTemplateString(template);
			return Render.StringToString(templateStr, getProps(pxConfiguration, uuid, vid, isMobileRequest, action));

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

		private static IDictionary<String, String> getProps(PxModuleConfigurationSection pxConfiguration, string uuid, string vid, bool isMobileRequest, string action)
		{
			IDictionary<String, String> props = new Dictionary<String, String>();
			string captchaParams = string.Format(CAPTCHA_QUERY_PARAMS, action, uuid, vid, isMobileRequest ? "1" : "0"); 
			props.Add("refId", uuid);
			props.Add("appId", pxConfiguration.AppId);
			props.Add("vid", vid);
			props.Add("uuid", uuid);
			props.Add("customLogo", pxConfiguration.CustomLogo);
			props.Add("cssRef", pxConfiguration.CssRef);
			props.Add("jsRef", pxConfiguration.JsRef);
			props.Add("logoVisibility", string.IsNullOrEmpty(pxConfiguration.CustomLogo) ? "hidden" : "visible");

			if (pxConfiguration.FirstPartyEnabled && !isMobileRequest)
			{
				props.Add("jsClientSrc", string.Format(CLIENT_SRC_FP, pxConfiguration.AppId.Substring(2)));
				props.Add("blockScript", string.Format(CAPTCHA_SRC_FP, pxConfiguration.AppId.Substring(2), captchaParams));
				props.Add("hostUrl", string.Format(HOST_FP, pxConfiguration.AppId.Substring(2)));
				props.Add("firstPartyEnabled", "1");
			}
			else
			{
				props.Add("jsClientSrc", string.Format(CLIENT_SRC_TP, Regex.Replace(pxConfiguration.ClientHostUrl, "https?:", ""), pxConfiguration.AppId));
				props.Add("hostUrl", string.Format(pxConfiguration.CollectorUrl, pxConfiguration.AppId));
				props.Add("blockScript", string.Format(CAPTCHA_SRC_TP, pxConfiguration.CaptchaHostUrl,  pxConfiguration.AppId, captchaParams));
			}
			return props;
		}
	}
}
