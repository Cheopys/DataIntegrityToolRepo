namespace DataIntegrityTool.Schema
{
	public class Administrators
	{
		public Int32 Id					{ get; set; }
		public string NameFirst			{ get; set; }
		public string NameLast			{ get; set; }
		public string Email				{ get; set; }
		public string PasswordHash		{ get; set; }
        public byte[]? AesKey			{ get; set; }
		public DateTime DateAdded		{ get; set; }
		public Int32? ChangePasswordToken		{ get; set; }
    }
}
