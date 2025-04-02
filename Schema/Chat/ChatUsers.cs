using System.Security.Cryptography;
using ProxChat.SharedObjectTypes;


namespace ProxChat.Schema
{
	public class ChatUsers
	{
		public Int32	Id			{ get; set; }
		public Int32	ChatId		{ get; set; }
		public Int64	UserId		{ get; set; }
		public DateTime TimeAdded	{ get; set; } = DateTime.UtcNow;
		public DateTime TimeMessage { get; set; } = DateTime.UtcNow;
	}
	public class UsersOnline
	{
		public Int32		 Id				{ get; set; }
		public Int64	     UserId			{ get; set; }
		public double	     Latitude		{ get; set; }
		public double	     Longitude		{ get; set; }
		public double	     Altitude		{ get; set; }
		public Int16		 Radius			{ get; set; }
		public DateTime		 TimeOnline		{ get; set; } = DateTime.UtcNow;
	}
};
