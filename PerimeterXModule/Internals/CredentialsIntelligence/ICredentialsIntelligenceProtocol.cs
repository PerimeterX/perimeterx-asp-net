using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace PerimeterX.Internals.CredentialsIntelligence
{
    public interface ICredentialsIntelligenceProtocol
    {
        LoginCredentialsFields ProcessCredentials(ExtractedCredentials extractedCredentials);
    }

    
}
