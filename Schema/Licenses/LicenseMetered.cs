namespace DataIntegrityTool.Schema
{
	public class LicenseMetered
	{
		public Int32 Id { get; set; }
		public Int32 CustomerId { get; set; }
		public String Hash { get; set; }
		public string? Project { get; set; }
		public Int32 Meter { get; set; }
	}
}
