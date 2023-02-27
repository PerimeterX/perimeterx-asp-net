using System;
using System.Web;
using PerimeterX.CustomBehavior;

namespace PerimeterX
{
    public class CustomLoginSuccessfulParser : ILoginSuccessfulParser
    {
        ILoginSuccessfulHandler loginSuccessfulParserHandler;

        public CustomLoginSuccessfulParser(PxModuleConfigurationSection config) 
        {
            loginSuccessfulParserHandler = PxCustomFunctions.GetCustomLoginSuccessfulHandler(config.CustomLoginSuccessfulHandler);
        }

        public bool IsLoginSuccessful(HttpResponse httpResponse)
        {
            try
            {
                if (loginSuccessfulParserHandler != null)
                {
                    return loginSuccessfulParserHandler.Handle(httpResponse);
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