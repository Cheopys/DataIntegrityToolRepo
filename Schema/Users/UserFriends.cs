namespace ProxChat.Schema
{
	public class UserFriends
	{
		public Int32 Id				{ get; set; }
		public Int64  userId		{ get; set; }
		public Int64  userIdFriend	{ get; set; }
	}
}
