
using Amazon.SimpleNotificationService.Model;

namespace DataIntegrityTool.Schema
{
	public class Session
	{
		public Int32		Id			{ get; set; }
		public Int32		CustomerId	{ get; set; }
		public Int32		ContentId	{ get; set; }
		public LicenseTypes Lcensetype	{ get; set; }
		public Int32		LicenseId	{ get; set; }
		public DateTime		timeBegin	{ get; set; }
	}
}
