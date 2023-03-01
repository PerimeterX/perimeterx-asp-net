
namespace PerimeterX
{
    public interface ICredentialsIntelligenceProtocol
    {
        LoginCredentialsFields ProcessCredentials(ExtractedCredentials extractedCredentials);
    }
}
