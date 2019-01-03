using System.Runtime.Serialization;

namespace PerimeterX
{
    public interface IActivityDetails
    {
        [DataMember(Name = "module_version")]
        string ModuleVersion { get; }
    }

    [DataContract]
    public class ActivityDetails : IActivityDetails
    {
        [DataMember(Name = "module_version")]
        public string ModuleVersion { get; internal set; }

        [DataMember(Name = "block_reason", EmitDefaultValue = true)]
        public BlockReasonEnum? BlockReason;

        [DataMember(Name = "block_uuid")]
        public string BlockUuid;

        [DataMember(Name = "client_uuid")]
        public string ClientUuid;

        [DataMember(Name = "block_score")]
        public int RiskScore;

        [DataMember(Name = "pass_reason", EmitDefaultValue = true)]
        public PassReasonEnum? PassReason;

        [DataMember(Name = "risk_rtt")]
        public long RiskRoundtripTime;

		[DataMember(Name = "block_action")]
		public string BlockAction;

	}

    [DataContract]
    public class EnforcerTelemetryActivityDetails : IActivityDetails
    {
        [DataMember(Name = "module_version")]
        public string ModuleVersion { get; internal set; }

        [DataMember(Name = "update_reason")]
        public EnforcerTelemetryUpdateReasonEnum UpdateReason;

        [DataMember(Name = "os_name")]
        public string OsName;

        [DataMember(Name = "node_name")]
        public string NodeName;

        [DataMember(Name = "enforcer_configs")]
        public string EnforcerConfigs;
    }
}