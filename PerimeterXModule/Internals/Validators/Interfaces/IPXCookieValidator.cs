namespace PerimeterX
{
	public interface IPXCookieValidator
	{
		bool Verify(PxContext context, IPxCookie pxCookie);
	}
}
