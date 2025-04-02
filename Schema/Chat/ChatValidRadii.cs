using ProxChat.SharedObjectTypes;

namespace ProxChat.Schema
{
	public class ChatRadii
	{
		public Int16		 Id				{ get; set; }
		public DistanceUnits distanceUnit	{ get; set; }
		public string		 description	{ get; set; }
		public float		 value			{ get; set; }
	}
}
