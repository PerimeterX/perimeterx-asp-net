using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerimeterX
{
    interface IDecodedCookie
    {
        string ExtractUUID();
        string ExtractVID();
        double ExtractTimestamp();
        string ExtractAction();
        double ExtractScore();
        bool IsCookieFormatValid();
    }
}
