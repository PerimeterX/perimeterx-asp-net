using System.Web;

namespace PerimeterX
{
    /// <summary>
    /// Custom verification handler, can be used to replace the default verification behavior 
    /// to execute custom logic based on the risk score returned by PerimeterX.
    /// </summary>
    public interface IVerificationHandler
    {
        void Handle(HttpApplication HttpApplication, PxContext pxContext, PxModuleConfigurationSection pxConfig);
    }
}
