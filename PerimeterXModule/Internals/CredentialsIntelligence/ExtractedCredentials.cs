using System.Runtime.Serialization;

namespace PerimeterX.Internals.CredentialsIntelligence
{
    public class ExtractedCredentials
    {
        [DataMember(Name = "username")]
        private string Username;

        [DataMember(Name = "password")]
        private string Password;

        public ExtractedCredentials(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }
    }
}
