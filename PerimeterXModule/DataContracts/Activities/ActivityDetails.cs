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

        [DataMember(Name = "http_method")]
        public string httpMethod;

        [DataMember(Name = "simulated_block")]
        public bool? SimulatedBlock;

        [DataMember(Name = "ci_version")]
        public string CiVersion { get; set; }

        [DataMember(Name = "credentials_compromised")]
        public bool CredentialsCompromised { get; set; }

        [DataMember(Name = "sso_step")]
        public string SsoStep { get; set; }

        [DataMember(Name = "request_id")]
        public string RequestId { get; set; }
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

    [DataContract]
    public class AdditionalS2SActivityDetails : IActivityDetails
    {

        [DataMember(Name = "client_uuid")]
        public string ClientUuid { get; set; }

        [DataMember(Name = "request_id")]
        public string RequestId { get; set; }

        [DataMember(Name = "ci_version")]
        public string CiVersion { get; set; }

        [DataMember(Name = "credentials_compromised", EmitDefaultValue = false)]
        public bool CredentialsCompromised { get; set; }

        [DataMember(Name = "http_status_code")]
        public int HttpStatusCode { get; set; }

        [DataMember(Name = "login_successful")]
        public bool LoginSuccessful { get; set; }

        [DataMember(Name = "raw_username")]
        public string RawUsername { get; set; }

        [DataMember(Name = "sso_step")]
        public string SsoStep { get; set; }

        [DataMember(Name = "module_version")]
        public string ModuleVersion { get; internal set; }
    }
}
