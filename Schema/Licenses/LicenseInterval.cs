namespace DataIntegrityTool.Schema
{
	public class LicenseInterval
	{
		public Int32 Id				{ get; set; }
		public Int32 CustomerId		{ get; set; }
		public String Hash			{ get; set; }
		public string? Project		{ get; set; }
		public DateTime Beginning	{ get; set; }
		public DateTime Ending		{ get; set; }
	}
}
