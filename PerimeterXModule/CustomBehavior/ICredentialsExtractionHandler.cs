using System.Web;

namespace PerimeterX.CustomBehavior
{
    public interface ICredentialsExtractionHandler
    {
        ExtractedCredentials Handle(HttpRequest httpRequest);
    }
}
