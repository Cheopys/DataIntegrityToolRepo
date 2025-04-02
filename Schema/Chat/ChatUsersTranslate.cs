using ProxChat.Schema;

namespace ProxChat.Schema
{
	public class ChatUserTranslate
	{
		public	Int64 Id				{ get; set; }
		public Int32 chatId				{ get; set; }
		public Int64 userIdRecipient	{ get; set; }
		public Int64 userIdSender		{ get; set; }
		public string? localeTranslate	{ get; set; } // null = no translation, otherwise the language to translate from
	}
}
