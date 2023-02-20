using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PerimeterX.CustomBehavior
{
    public interface ILoginSuccessful
    {
        void Handle(HttpApplication HttpApplication, PxContext pxContext, PxModuleConfigurationSection pxConfig);
    }
}
