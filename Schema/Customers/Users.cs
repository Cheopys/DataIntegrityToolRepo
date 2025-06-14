namespace DataIntegrityTool.Schema
{
	public class Users
	{
		public Int32 Id					{ get; set; }
		public Int32 CustomerId			{ get; set; }
		public Int32  UserID			{ get; set; }
		public string Name				{ get; set; }
		public string Email				{ get; set; }
		public string PasswordHash		{ get; set; }
		public List<ToolTypes> Tools	{ get; set; }
        public byte[]? AesKey			{ get; set; }
		public DateTime DateAdded		{ get; set; }
		public Int32? ChangePasswordToken		{ get; set; }
    }
}
