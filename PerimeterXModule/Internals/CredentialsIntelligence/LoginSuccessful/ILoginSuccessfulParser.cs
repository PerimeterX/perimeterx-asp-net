using System.Web;

namespace PerimeterX
{
    public interface ILoginSuccessfulParser
    {
        bool IsLoginSuccessful(HttpResponse httpResponse);
    }
}