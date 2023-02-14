namespace PerimeterX
{
    public class MultistepSSoCredentialsIntelligenceProtocol : ICredentialsIntelligenceProtocol
    {
        public LoginCredentialsFields ProcessCredentials(ExtractedCredentials extractedCredentials)
        {
            string rawUsername = null;
            string password = null;
            string ssoStep = "pass";

            if (extractedCredentials.Username != null) 
            {
                rawUsername = extractedCredentials.Username;
                ssoStep = "user";
            } 

            if (extractedCredentials.Password != null)
            {
                password = PxCommonUtils.Sha256(extractedCredentials.Password);
            } 

            return new LoginCredentialsFields(
                rawUsername, 
                password, 
                rawUsername, 
                "multistep_sso",
                ssoStep
            );
        }
    }
}
