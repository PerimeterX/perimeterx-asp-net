using PerimeterX.DataContracts.Cookies.Base;

namespace PerimeterX.DataContracts.Cookies.Interface
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