using System.Web;
using System.Text.RegularExpressions;

namespace PerimeterX
{
    public class BodyLoginSuccessfulParser : ILoginSuccessfulParser
    {
        private readonly string bodyRegex;
     
        public BodyLoginSuccessfulParser(PxModuleConfigurationSection config) {
            bodyRegex = config.LoginSuccessfulBodyRegex;
        }

        public bool IsLoginSuccessful(HttpResponse httpResponse)
        {
            string body = ((OutputFilterStream)httpResponse.Filter).ReadStream();

            if (body == null) {
                return false;
            }

            return Regex.IsMatch(body, bodyRegex);       
        }
    }
}