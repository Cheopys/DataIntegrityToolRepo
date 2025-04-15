namespace DataIntegrityTool.Schema
{
	public class UsersAwaitingMFA : Users
    {
        public Int32 Id { get; set; }
		public Int32  MFA { get; set; }
    }
}
