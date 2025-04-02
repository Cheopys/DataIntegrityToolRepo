using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using ProxChat.SharedObjectTypes;
using ProxChat.Db;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace ProxChat.Schema
{
    public class Users
    {
		public Int64        Id			    { get; set; }
		public Int32       currentProfileId  { get; set; }
        public string?      firstName       { get; set; }
        public string?      lastName        { get; set; }
        public string?      email           { get; set; }
		public string?		phoneNumber		{ get; set; }
		public DateTime?	birthdayDate	{ get; set; }
        public Relationship? relationshipId  { get; set; }
		public Pronouns? pronoun				{ get; set; }
		public string?       locale			{ get; set; }	
		public DistanceUnits? units			{ get; set; }
		public Int16?	recentRadius	{ get; set; }
		public string		passwordHash	{ get; set; }
		public DateTime		TimeNotifications { get; set; }

		// keys
		public byte[]	aeskey	{ get; set; }
		public byte[]   aesiv	{ get; set; }
	}
}
