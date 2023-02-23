using System.IO;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PerimeterX
{
    public class BodyReader
    {
        public static string ReadRequestBodyAsync(HttpRequest request)
        {
            MemoryStream memstream = new MemoryStream();
            request.InputStream.CopyTo(memstream);
            memstream.Position = 0;
            using (StreamReader reader = new StreamReader(memstream))
            {
                string text = reader.ReadToEnd();
                request.InputStream.Position = 0;
                return text;
            }
        }
    }
}