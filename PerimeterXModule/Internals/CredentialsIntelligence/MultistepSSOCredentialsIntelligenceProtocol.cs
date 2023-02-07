using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX.Internals.CredentialsIntelligence
{

    public class MultistepSSoCredentialsIntelligenceProtocol : ICredentialsIntelligenceProtocol
    {
        public LoginCredentialsFields ProcessCredentials(ExtractedCredentials extractedCredentials)
        {
            string rawUsername = null;
            string password = null;

            if (extractedCredentials.Username != null) 
            {
                rawUsername = extractedCredentials.Username;
            } 

            if (extractedCredentials.Password != null)
            {
                password = PxCommonUtils.Sha256(extractedCredentials.Password);
            } 

            return new LoginCredentialsFields(
                rawUsername, 
                password, 
                rawUsername, 
                "multistep_sso"
            );
        }
  
    }
}
