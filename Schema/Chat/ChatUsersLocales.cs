namespace ProxChat.Schema
{
	public class ChatUsersLocales
	{
		public Int64 Id				{ get; set; }
		public Int32 ChatId			{ get; set; }
		public Int64 userId			{ get; set; }
		public Int16 localeInId		{ get; set;}
		public Int16 localeOutId	{ get; set; }
	}
}
