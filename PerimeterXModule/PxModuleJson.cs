using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PerimeterX
{
    public static class PxModuleJson
    {
        public static string StringifyObject<T>(T obj)
        {
            string json;
            using (MemoryStream s = new MemoryStream())
            using (StreamReader streamReader = new StreamReader(s))
            {
                DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings()
                {
                    UseSimpleDictionaryFormat = true
                };
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T), settings);
                ser.WriteObject(s, obj);
                s.Position = 0;
                json = streamReader.ReadToEnd();
            }
            return json;
        }

        public static T ParseObject<T>(string data)
        {
            T obj;
            using (MemoryStream s = new MemoryStream(Encoding.Unicode.GetBytes(data)))
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                obj = (T)ser.ReadObject(s);
            }
            return obj;
        }
    }
}
