using ProxChat.Schema;

namespace ProxChat.Schema
{
	public class DirectMessages
	{
		public Int32	Id				{ get; set; }
		public Int64	userIdSender	{ get; set; }
		public Int64	userIdRecipient	{ get; set; }
		public string	message			{ get; set; }
		public bool		hasImage		{ get; set; } = false;
		public DateTime timeSent		{ get; set; }
	}
}
