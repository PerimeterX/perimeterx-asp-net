namespace PerimeterX
{
    public class MultistepSSoCredentialsIntelligenceProtocol : ICredentialsIntelligenceProtocol
    {
        public LoginCredentialsFields ProcessCredentials(ExtractedCredentials extractedCredentials)
        {
            string rawUsername = null;
            string password = null;
            string ssoStep = MultistepSsoStep.PASSWORD;

            if (extractedCredentials.Username != null && extractedCredentials.Username.Length > 0) 
            {
                rawUsername = extractedCredentials.Username;
                ssoStep = MultistepSsoStep.USER;
            } 

            if (extractedCredentials.Password != null && extractedCredentials.Password.Length > 0)
            {
                password = PxCommonUtils.Sha256(extractedCredentials.Password);
            } 

            if (rawUsername == null && password == null)
            {
                return null;
            }

            return new LoginCredentialsFields(
                rawUsername, 
                password, 
                rawUsername, 
                CIVersion.MULTISTEP_SSO,
                ssoStep
            );
        }
    }
}
