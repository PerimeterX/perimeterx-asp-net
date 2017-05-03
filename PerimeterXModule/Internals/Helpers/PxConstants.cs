using System.Diagnostics;
using System.Runtime.Serialization;
using Jil;


namespace PerimeterX
{

    public static class PxConstants
    {
        public static readonly string HEX_ALPHABET = "0123456789abcdef";
        public static readonly string[] PX_COOKIES_PREFIX = { COOKIE_V1_PREFIX, COOKIE_V3_PREFIX };
        public const string COOKIE_V1_PREFIX = "_px";
        public const string COOKIE_V3_PREFIX = "_px3";
        public static readonly string COOKIE_CAPTCHA_PREFIX = "_pxCaptcha";
        public static readonly string PX_VALIDATED_HEADER = "X-PX-VALIDATED";
        public static readonly string CONFIG_SECTION = "perimeterX/pxModuleConfigurationSection";
        public static readonly string LOG_CATEGORY = "PxModule";
        public static readonly string MODULE_VERSION = GetAssemblyVersion();
        public static readonly Options JSON_OPTIONS = new Options(prettyPrint: false, excludeNulls: true, includeInherited: true);


		// Endpoints
		public const string RISK_API_V2 = "/api/v2/risk";
        public const string CAPTCHA_API_V1 = "/api/v1/risk/captcha";

        private static string GetAssemblyVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }
    }   

    [DataContract]
    public enum RiskRequestReasonEnum
    {
        [EnumMember(Value = "none")]
        NONE,
        [EnumMember(Value = "no_cookie")]
        NO_COOKIE,
        [EnumMember(Value = "expired_cookie")]
        EXPIRED_COOKIE,
        [EnumMember(Value = "invalid_cookie")]
        INVALID_COOKIE,
        [EnumMember(Value = "cookie_decryption_failed")]
        DECRYPTION_FAILED,
        [EnumMember(Value = "cookie_validation_failed")]
        VALIDATION_FAILED
    }

    [DataContract]
    public enum BlockReasonEnum
    {
        [EnumMember(Value = "none")]
        NONE,
        [EnumMember(Value = "cookie_high_score")]
        COOKIE_HIGH_SCORE,
        [EnumMember(Value = "risk_high_score")]
        RISK_HIGH_SCORE
    }

    [DataContract]
    public enum ModuleMode
    {
		[EnumMember(Value = "monitor_mode")]
		MONITOR_MODE = 1,
		
        [EnumMember(Value = "block_mode")]
		BLOCK_MODE = 0
    }
}
