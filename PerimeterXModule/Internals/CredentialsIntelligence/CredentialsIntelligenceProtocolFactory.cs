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
                case ("v2"):
                    return new V2CredentialsIntelligenceProtocol();
                case ("multistep_sso"):
                    return new MultistepSSoCredentialsIntelligenceProtocol();
                default:
                    throw new Exception("Unknown CI protocol version" + protocolVersion);
            }
        }
    }
}
