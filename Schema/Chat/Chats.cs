using ProxChat.SharedObjectTypes;

namespace ProxChat.Schema
{
    public class Chats
    {
        public Int32        Id			{ get; set; }
        public string       Name		{ get; set; }
        public double       Latitude	{ get; set; }
		public double       Longitude	{ get; set; }
		public double       Altitude	{ get; set; }
        public Int16        RadiusId	{ get; set; }
		public DateTime     TimeCreated	{ get; set; }
		public DateTime		TimeMessage	{ get; set; }
//		public string       TopicARN	{ get; set; }
//		public string       QueueARN    { get; set; }
	}
}
