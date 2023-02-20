namespace PerimeterX
{
    public class MultistepSSoCredentialsIntelligenceProtocol : ICredentialsIntelligenceProtocol
    {
        public LoginCredentialsFields ProcessCredentials(ExtractedCredentials extractedCredentials)
        {
            string rawUsername = null;
            string password = null;
            string ssoStep = MultistepSsoStep.PASSWORD;

            if (extractedCredentials.Username != null) 
            {
                rawUsername = extractedCredentials.Username;
                ssoStep = MultistepSsoStep.USER;
            } 

            if (extractedCredentials.Password != null)
            {
                password = PxCommonUtils.Sha256(extractedCredentials.Password);
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
