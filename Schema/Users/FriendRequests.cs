using ProxChat.SharedObjectTypes;

namespace ProxChat.Schema	
{
	public class FriendRequests
	{
		public Int32    Id		  { get; set; }
		public Int64	UserId	  { get; set; }
		public Int64    FriendId  { get; set; }
		public DateTime TimeSent { get; set; }
	}
}
