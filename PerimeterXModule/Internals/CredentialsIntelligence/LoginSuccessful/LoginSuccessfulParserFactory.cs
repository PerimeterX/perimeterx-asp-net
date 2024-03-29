﻿
namespace PerimeterX
{
    public class LoginSuccessfulParserFactory
    {
        public static ILoginSuccessfulParser Create(PxModuleConfigurationSection config)
        {
            switch (config.LoginSuccessfulReportingMethod)
            {
                case ("body"):
                    return new BodyLoginSuccessfulParser(config);
                case ("header"):
                    return new HeaderLoginSuccessfulParser(config);
                case ("status"):
                    return new StatusLoginSuccessfulParser(config);
                case ("custom"):
                    return new CustomLoginSuccessfulParser(config);
                default:
                    return null;
            }
        }
    }
}
