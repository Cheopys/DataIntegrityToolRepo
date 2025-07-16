using System.ComponentModel.DataAnnotations.Schema;
using System.Composition.Convention;
using System.Diagnostics.Contracts;
using System.Runtime.Intrinsics.Arm;
using DataIntegrityTool.Db;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace DataIntegrityTool.Schema
{
    public class Customers
    {
        public int     Id             { get; set; }
		public string   NameFirst     { get; set; }
		public string   NameLast      { get; set; }
		public string   Company       { get; set; }
		public string   Email         { get; set; }
        public string   PasswordHash  { get; set; }
        public DateTime DateAdded     { get; set; }
        public string   Notes         { get; set; }
        public List<ToolTypes>? Tools { get; set; }
        public byte[]?  AesKey        { get; set; }
        public DateTime UsageSince    { get; set; } = DateTime.MinValue;
        public Int16    SeatsMax      { get; set; }
		public Int32    Scans         { get; set; }
		public TimeSpan? SubscriptionTime         { get; set; }
    }
}
