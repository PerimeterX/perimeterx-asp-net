using System.Runtime.Serialization;

namespace PerimeterX
{
    public class LoginCredentialsFields
    {
        [DataMember(Name = "username")]
        public string Username;

        [DataMember(Name = "password")]
        public string Password;

        [DataMember(Name = "rawUsername", IsRequired = false)]
        public string RawUsername;

        [DataMember(Name = "version")]
        public string CiVersion;

        [DataMember(Name = "ssoStep", IsRequired = false)]
        public string SsoStep;

        public LoginCredentialsFields(string username, string password, string rawUsername, string ciVersion, string SsoStep)
        {
            this.Username = username;
            this.Password = password;
            this.RawUsername = rawUsername;
            this.CiVersion = ciVersion;
            this.SsoStep = SsoStep;
        }

        public LoginCredentialsFields(string username, string password, string rawUsername, string version)
        {
            this.Username = username;
            this.Password = password;
            this.RawUsername = rawUsername;
            this.CiVersion = version;
        }
    }
}
