namespace DataIntegrityTool.Schema
{
	public class LicenseMetered
	{
		public Int32 Id			 { get; set; }
		public Int32 CustomerId	 { get; set; }
        public Int32 UserId		 { get; set; }
		public Int32 Count		 { get; set; }
		public DateTime TimeBegun{ get; set; } = DateTime.MinValue;
    }
}
