using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Contracts;
using DataIntegrityTool.Db;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace DataIntegrityTool.Schema
{
    public class Licenses
    {
		public Int32 Id { get; set; }
	}
}