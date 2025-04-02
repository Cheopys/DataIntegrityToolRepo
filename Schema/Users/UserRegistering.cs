namespace ProxChat.Schema
{
	public class UserRegistering
	{
		public Int64 Id		 { get; set; }
		public byte[] aesKey { get; set; }
		public byte[] aesIV  { get; set; }
	}
}
