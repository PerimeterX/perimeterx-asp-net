using System.Runtime.Serialization;

namespace PerimeterX
{
    [DataContract]
    public class ActivityDetails
    {
        [DataMember(Name = "block_reason")]
        public BlockReasonEnum BlockReason;

        [DataMember(Name = "block_uuid")]
        public string BlockUuid;
    }

}