using System.Linq;
using System.Web;
using System.Collections.Generic;

namespace PerimeterX
{
    public class StatusLoginSuccessfulParser : ILoginSuccessfulParser
    {
        private readonly List<string> successfulStatuses;

        public StatusLoginSuccessfulParser(PxModuleConfigurationSection config)
        {
            successfulStatuses = config.LoginSuccessfulStatus.Cast<string>().ToList();
        }

        public bool IsLoginSuccessful(HttpResponse httpResponse)
        {
            return successfulStatuses.Contains(httpResponse.StatusCode.ToString());
        }
    }
}