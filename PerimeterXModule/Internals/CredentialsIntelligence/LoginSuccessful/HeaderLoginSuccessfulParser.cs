using System.Web;

namespace PerimeterX
{
    public class HeaderLoginSuccessfulParser : ILoginSuccessfulParser
    {
        private readonly string successfulHeaderName;
        private readonly string successfulHeaderValue;

        public HeaderLoginSuccessfulParser(PxModuleConfigurationSection config)
        {
            successfulHeaderName = config.LoginSuccessfulHeaderName;
            successfulHeaderValue = config.LoginSuccessfulHeaderValue;
        }

        public bool? IsLoginSuccessful(HttpResponse httpResponse)
        {
            return httpResponse.Headers[successfulHeaderName] == successfulHeaderValue;
        }
    }
}
