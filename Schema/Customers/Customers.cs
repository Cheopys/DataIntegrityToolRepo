using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using System.Runtime.Intrinsics.Arm;
using DataIntegrityTool.Db;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace DataIntegrityTool.Schema
{
    public class Customers
    {
        public int     Id                       { get; set; }
		public string   Name                    { get; set; }
        public string   Description             { get; set; }
        public string   Email                   { get; set; }
        public string   PasswordHash            { get; set; }
        public DateTime DateAdded               { get; set; }
        public string   Notes                   { get; set; }
        public List<ToolTypes>? Tools           { get; set; }
        public byte[]   AesKey                  { get; set; }
        public DateTime UsageSince              { get; set; } = DateTime.MinValue;
        public DateTime SubscriptionEnd         { get; set; }
    }
}
