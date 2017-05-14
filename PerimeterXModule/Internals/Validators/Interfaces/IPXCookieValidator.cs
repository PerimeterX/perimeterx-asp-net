namespace PerimeterX
{
    interface IPXCookieValidator
    {
        bool CookieVerify(PxContext context, IPxCookie pxCookie);
    }
}
