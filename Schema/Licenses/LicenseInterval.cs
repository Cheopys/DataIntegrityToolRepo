namespace DataIntegrityTool.Schema
{
	public class LicenseInterval
	{
		public Int32    Id			{ get; set; }
		public Int32    UserId		{ get; set; }
        public Int32    CustomerId { get; set; }
		public DateTime? TimeBegin	{ get; set; }
        public DateTime  TimeEnd		{ get; set; }
    }
}
