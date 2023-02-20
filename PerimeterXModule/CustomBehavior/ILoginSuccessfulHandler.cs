using System.Web;

namespace PerimeterX.CustomBehavior
{
    public interface ILoginSuccessfulHandler
    {
        bool Handle(HttpApplication HttpApplication);
    }
}
