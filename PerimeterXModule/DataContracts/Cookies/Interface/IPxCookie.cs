namespace PerimeterX
{
    public interface IPxCookie
    {
        bool Deserialize();

        bool IsSecured(string userAgent, string cookieKey, bool signedWithIP = false, string ip = "");

        double Score { get; }
        string Uuid { get; }
        string Vid { get; }
        string BlockAction { get; }
        string Hmac { get; }
        double Timestamp { get; }
        BaseDecodedCookie DecodedCookie { get; }
    }
}