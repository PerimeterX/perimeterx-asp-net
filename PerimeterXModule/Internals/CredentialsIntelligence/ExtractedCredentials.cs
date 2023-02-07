using System.Runtime.Serialization;

namespace PerimeterX.Internals.CredentialsIntelligence
{
    public class ExtractedCredentials
    {
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "password")]
        public string Password { get; set; }

        public ExtractedCredentials(string username, string password)
        {
            this.Username = username;
            this.Password = password;
        }
    }
}
