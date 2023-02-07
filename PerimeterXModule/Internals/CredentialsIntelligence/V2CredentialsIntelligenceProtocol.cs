using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX.Internals.CredentialsIntelligence
{

    public class V2CredentialsIntelligenceProtocol : ICredentialsIntelligenceProtocol
    {
        public LoginCredentialsFields ProcessCredentials(ExtractedCredentials extractedCredentials)
        {
            string normalizedUsername = PxCommonUtils.IsEmailAddress(extractedCredentials.Username) ? NormalizeEmailAddress(extractedCredentials.Username) : extractedCredentials.Username;
            string hashedUsername = PxCommonUtils.Sha256(normalizedUsername);
            string hashedPassword = HashPassword(hashedUsername, extractedCredentials.Password);

            return new LoginCredentialsFields(
                hashedUsername,
                hashedPassword,
                extractedCredentials.Username,
                "v2"
            );
        }

        public static string NormalizeEmailAddress(string emailAddress)
        {
            string lowercaseEmail = emailAddress.Trim().ToLower();
            int atIndex = lowercaseEmail.IndexOf("@");
            string normalizedUsername = lowercaseEmail.Substring(0, atIndex);
            int plusIndex = normalizedUsername.IndexOf("+");

            if (plusIndex > -1)
            {
                normalizedUsername = normalizedUsername.Substring(0, plusIndex);
            }

            string domain = lowercaseEmail.Substring(atIndex);

            if (domain == "@gmail.com")
            {
                normalizedUsername = normalizedUsername.Replace(".", "");
            }

            return normalizedUsername;
        }

        public static string HashPassword(string salt, string password)
        {
            string hashedPassword = PxCommonUtils.Sha256(password);
            return PxCommonUtils.Sha256(salt + hashedPassword);
        }

    }
}
