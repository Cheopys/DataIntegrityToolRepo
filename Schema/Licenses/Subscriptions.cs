namespace DataIntegrityTool.Schema
{
	public class Subscriptions
	{
		public Int32    Id				{ get; set; }
		public Int32     CustomerId		{ get; set; }
		public DateTime? ExpirationDate	{ get; set; }
	}
}
