namespace DataIntegrityTool.Schema
{
	public class SubscriptionTypes
	{
		public Int32				Id		 { get; set; }
		public TimeSpan				duration { get; set; }
		public Int32				scans	 { get; set; }
		public string				level	 { get; set; }
	};
}
