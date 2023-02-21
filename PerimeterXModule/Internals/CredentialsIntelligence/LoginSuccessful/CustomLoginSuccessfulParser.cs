using System;
using System.Web;
using PerimeterX.CustomBehavior;

namespace PerimeterX
{
    public class CustomLoginSuccessfulParser : ILoginSuccessfulParser
    {
        ILoginSuccessfulHandler loginSuccessfulParserhandler;

        public CustomLoginSuccessfulParser(PxModuleConfigurationSection config) 
        {
            loginSuccessfulParserhandler = PxCustomFunctions.GetCustomLoginSuccessfulHandler(config.CustomLoginSuccessfulHandler);
        }

        public bool IsLoginSuccessful(HttpResponse httpResponse)
        {
            try
            {
                if (loginSuccessfulParserhandler != null)
                {
                    return loginSuccessfulParserhandler.Handle(httpResponse);
                } 
            }
            catch (Exception ex)
            {
                PxLoggingUtils.LogDebug("An error occurred while executing login successful handler " + ex.Message);
                
            }

            return false;
        }
    }
}