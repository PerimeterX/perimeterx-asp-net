using System;
using PerimeterX;

namespace PerimeterX
{
    public class CredentialsIntelligenceProtocolFactory
    {
        public static ICredentialsIntelligenceProtocol Create(string protocolVersion)
        {
            switch(protocolVersion)
            {
                case CIVersion.V2:
                    return new V2CredentialsIntelligenceProtocol();
                case CIVersion.MULTISTEP_SSO:
                    return new MultistepSSoCredentialsIntelligenceProtocol();
                default:
                    throw new Exception("Unknown CI protocol version: " + protocolVersion);
            }
        }
    }
}
