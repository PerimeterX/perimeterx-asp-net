using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX.DataContracts.Cookies
{
    public interface BaseDecodedCookie
    {
        string GetHMAC();
        string GetUUID();
        string GetVID();
        string GetBlockAction();
        double GetTimestamp();
        double GetScore();

        bool IsCookieFormatValid();
    }
}
