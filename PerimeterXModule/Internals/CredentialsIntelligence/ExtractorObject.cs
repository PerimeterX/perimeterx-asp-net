using System.Runtime.Serialization;

namespace PerimeterX
{
   [DataContract]
   public class ExtractorObject
   {
        [DataMember(Name = "path")]
        public string Path;

        [DataMember(Name = "path_type")]
        public string PathType;

        [DataMember(Name = "method")]
        public string Method;

        [DataMember(Name = "sent_through")]
        public string SentThrough;

        [DataMember(Name = "pass_field")]
        public string PassFieldName;

        [DataMember(Name = "user_field")]
        public string UserFieldName;
    }
}
