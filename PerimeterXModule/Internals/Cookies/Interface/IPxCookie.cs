namespace PerimeterX
{
	public interface IPxCookie
	{
		bool Deserialize();

		bool IsSecured(string cookieKey, string[] additionalFields);

		int Score { get; }
		string Uuid { get; }
		string Vid { get; }
		string BlockAction { get; }
		string Hmac { get; }
		double Timestamp { get; }
		object DecodedCookie { get; }
	}
}