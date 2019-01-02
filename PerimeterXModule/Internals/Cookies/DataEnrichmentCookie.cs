using System.Text;

namespace PerimeterX.DataContracts.Cookies
{
	public sealed class DataEnrichmentCookie
	{
		private bool isValid = false;
		private dynamic jsonPayload;

		public bool IsValid { set { isValid = value; } get { return isValid; } }
		public dynamic JsonPayload { set { jsonPayload = value; } get { return jsonPayload; } }
		
		public DataEnrichmentCookie(dynamic jsonPayload, bool isValid)
		{
			this.jsonPayload = jsonPayload;
			this.isValid = isValid;
		}
	}
}
