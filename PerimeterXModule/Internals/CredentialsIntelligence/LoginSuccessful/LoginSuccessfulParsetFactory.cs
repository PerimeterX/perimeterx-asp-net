
namespace PerimeterX
{
    public class LoginSuccessfulParsetFactory
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
                default:
                    return null;
            }
        }
    }
}
