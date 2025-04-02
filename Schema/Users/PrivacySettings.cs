namespace ProxChat.Schema
{
    public class PrivacySettings
	{
		public Int32  Id					{ get; set; }
		public Int64  UserId				{ get; set; }
		public bool firstNamePrivate			{ get; set; } = true;
		public bool lastNamePrivate			{ get; set; } = true;
		public bool emailPrivate			{ get; set; } = true;
		public bool birthdayPrivate			{ get; set; } = true;
		public bool pronounPrivate			{ get; set; } = true;
		public bool relationshipPrivate		{ get; set; } = true;
	}
}
