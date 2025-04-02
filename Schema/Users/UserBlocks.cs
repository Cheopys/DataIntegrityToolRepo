using System.ComponentModel.DataAnnotations.Schema;

namespace ProxChat.Schema
{
	public class UserBlocks
	{
		public Int64 Id { get; set; }
		public Int64   userId		{ get; set; }
		public Int64   UserIdBlocked{ get; set; }
	}
}
