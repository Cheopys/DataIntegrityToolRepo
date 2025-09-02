namespace DataIntegrityTool.Schema
{
	public class CustomerPayments
	{
		public Int32	Id					{ get; set; }
		public Int32	CustomerId			{ get; set; }
		public DateTime Date				{ get; set; }
		public Int32	Amount				{ get; set; }
		public Int32?   SubscriptionType	{ get; set; }
		public Int16?   Scans				{ get; set; }
	}
}
