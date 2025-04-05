using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using DataIntegrityTool.Db;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace DataIntegrityTool.Schema
{
    public class Customers
    {
        public int Id               { get; set; }
        public string Unique        { get; set; }
		public string Name          { get; set; }
        public string Description   { get; set; }
        public string EmailContact  { get; set; }
        public string Notes         { get; set; }
        public DateTime DateAdded   { get; set; }
    }
}
