using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProxChat.Schema
{
	[Table("TranslationOptOuts	")]
	public class TranslationOptOut
	{
		[Key]
		public Int32	Id			 { get; set; }
		public Int64	UserId		 { get; set; }
		public string   LocaleOptOut { get; set; }
	}
}
