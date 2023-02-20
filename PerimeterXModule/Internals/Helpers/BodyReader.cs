using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace PerimeterX
{
    public class BodyReader
    {

        public static async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            HttpRequest httpRequest = request;

            using (var reader = new StreamReader(httpRequest.InputStream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}