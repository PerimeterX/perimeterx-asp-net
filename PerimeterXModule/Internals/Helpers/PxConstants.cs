using System;
using System.Runtime.Serialization;

namespace PerimeterX
{

    public static class PxConstants
    {
        public readonly static string HEX_ALPHABET = "0123456789abcdef";
        public readonly static string[] PX_COOKIES_PREFIX = { COOKIE_V1_PREFIX, COOKIE_V3_PREFIX };
        public const string COOKIE_V1_PREFIX = "_px";
        public const string COOKIE_V3_PREFIX = "_px3";
        public const string COOKIE_CAPTCHA_PREFIX = "_pxCaptcha";
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
	public class ActivityDetails
	{
		[DataMember(Name = "block_reason")]
		public BlockReasonEnum BlockReason;

		[DataMember(Name = "block_uuid")]
		public string BlockUuid;
	}
}
