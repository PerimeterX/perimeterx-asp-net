using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace PerimeterX
{
    public class BodyReader
    {
        public static string ReadRequestBody(HttpRequest request)
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

        public static Dictionary<string, string> GetFormDataContentAsDictionary(string body, string contentType)
        {
            var formData = new Dictionary<string, string>();

            var boundary = contentType.Split(';')
                  .SingleOrDefault(x => x.Trim().StartsWith("boundary="))?
                  .Split('=')[1];

            var parts = body.Split(new[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var lines = part.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                if (part.StartsWith("--"))
                {
                    continue;
                }

                var key = string.Empty;
                var value = new StringBuilder();
                foreach (var line in lines)
                {
                    if (line.StartsWith("Content-Disposition"))
                    {
                        key = line.Split(new[] { "name=" }, StringSplitOptions.RemoveEmptyEntries)[1].Trim('\"');
                    }
                    else
                    {
                        value.Append(line);
                    }
                }

                formData.Add(key, value.ToString());
            }

            return formData;
        }
    }
}