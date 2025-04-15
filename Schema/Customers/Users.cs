namespace DataIntegrityTool.Schema
{
	public class Users
	{
		public Int32 Id					{ get; set; }
		public Int32 CompanyId			{ get; set; }
		public Int32  UserID			{ get; set; }
		public string Name				{ get; set; }
		public string Email				{ get; set; }
		public string PasswordHash		{ get; set; }
		public string? MFA				{ get; set; }
		public List<ToolTypes> Tools	{ get; set; }
        public byte[] aeskey			{ get; set; }
		public DateTime DateAdded		{ get; set; }
    }
}
