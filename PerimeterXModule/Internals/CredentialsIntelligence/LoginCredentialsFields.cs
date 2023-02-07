using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX.Internals.CredentialsIntelligence
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
        public string Version;

        [DataMember(Name = "ssoStep", IsRequired = false)]
        public string SsoStep;

        public LoginCredentialsFields(string username, string password, string rawUsername, string version, string SsoStep)
        {
            this.Username = username;
            this.Password = password;
            this.RawUsername = rawUsername;
            this.Version = version;
            this.SsoStep = SsoStep;
        }

        public LoginCredentialsFields(string username, string password, string rawUsername, string version)
        {
            this.Username = username;
            this.Password = password;
            this.RawUsername = rawUsername;
            this.Version = version;
        }
    }
}
