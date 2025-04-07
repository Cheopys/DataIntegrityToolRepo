namespace DataIntegrityTool.Schema
{
	public class Logins
	{
		public Int32 Id					{ get; set; }
		public Int32 CompanyId			{ get; set; }
		public string Email				{ get; set; }
		public string PasswordHash		{ get; set; }
		public string? MFA				{ get; set; }
		public List<ToolTypes> tools	{ get; set; }
	}
}
