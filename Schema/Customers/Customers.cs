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
        public string   EmailContact            { get; set; }
        public DateTime DateAdded               { get; set; }
        public string   Notes                   { get; set; }
        public Int32 LicensingMeteredCount      { get; set; }
        public Int32 LicensingIntervalSeconds   { get; set; }
        public bool     UserLicensingPool       { get; set; }
        public byte[]   aeskey                  { get; set; }
        public DateTime usageSince             { get; set; } = DateTime.MinValue;
    }
}
