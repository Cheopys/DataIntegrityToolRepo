using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProxChat.Schema
{
	[Table("UserRatings	")]
	public class UserRatings
	{
		[Key]
		public Int32	Id			{ get; set; }
		public Int64	UserId		{ get; set; }
		public Int64	UserIdRated { get; set; }
		public Int16	Rating		{ get; set; }
		public string?	Notes		{ get; set; }
	}
}
