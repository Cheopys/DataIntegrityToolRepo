namespace ProxChat.Schema
{
	public class ChatMessages
	{
		public Int64	Id			{ get; set; }
		public Int32	ChatId		{ get; set; }
		public Int64	UserIdSender{ get; set; }
		public string	Message		{ get; set; }
		public DateTime TimeSent	{ get; set; }
		public bool		hasImage	{ get; set; } = false;

		public ChatMessages()
		{
			Message = string.Empty;
		}
	}
}
