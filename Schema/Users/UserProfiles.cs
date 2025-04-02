using ProxChat.Db;
using ProxChat.SharedObjectTypes;

namespace ProxChat.Schema
{
	public class UserProfiles
	{
		public Int32		Id				{ get; set; }
		public Int64		userId			{ get; set; }
		public UserProfileType profileType	{ get; set; }
		public bool?		isCurrent		{ get; set; }
		public string		name			{ get; set; } // profile name
		public string?		moniker			{ get; set; }
		public string?		email			{ get; set; }
		public Int32?		privacyId		{ get; set; }
		public List<Int32>?  userInterestIDs	{ get; set; }
		public bool?		avatar			{ get; set; } = false;
		public string?		Employer		{ get; set; }
		public string?		Title			{ get; set; }
		public string?		Responsibilities{ get; set; }

		public UserProfiles()
		{
			userInterestIDs = new();
		}
	}

}
