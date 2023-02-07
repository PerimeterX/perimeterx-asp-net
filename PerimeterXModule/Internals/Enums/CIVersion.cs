using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
    [DataContract]
    public enum CIVersion
    {
        [EnumMember(Value = "v2")]
        V2,
        [EnumMember(Value = "multistep_sso")]
        MULTISTEP_SSO
    }
}
