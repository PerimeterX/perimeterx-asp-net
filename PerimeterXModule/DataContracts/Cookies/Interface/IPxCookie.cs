namespace PerimeterX
{
    public interface IPxCookie
    {
        bool Deserialize();
        string GetDecodedCookieHMAC();
        BaseDecodedCookie GetDecodedCookie();
        bool IsCookieHighScore();
        bool IsExpired();
        bool IsSecured();
    }
}