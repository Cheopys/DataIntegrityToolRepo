
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using DataIntegrityTool.Schema;
using System.Data;
using EFCore.NamingConventions;

namespace DataIntegrityTool.Db
{
    public class DataContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public DataContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
			options.UseNpgsql("Host=dataintegritytool.ct6cykcgeval.ca-central-1.rds.amazonaws.com;Port=5432; Database=dataintegritytool;Username=postgres;Password=YD4NKpMxscgQcFsSN8NA6y5;Include Error Detail=true");
        }

		public DbSet<Customers>      Customers          { get; set; }
		public DbSet<Content>        Content            { get; set; }
		public DbSet<Licenses>       Licenses           { get; set; }
		public DbSet<LicenseMetered> LicenseMetered     { get; set; }
		public DbSet<LicenseInterval> LicenseInterval   { get; set; }
        public DbSet<Session>        Session            { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
			modelBuilder.Entity<Customers>      ().ToTable("Customers");
			modelBuilder.Entity<Content>        ().ToTable("Content");
            modelBuilder.Entity<Licenses>       ().ToTable("Licenses");
			modelBuilder.Entity<LicenseMetered> ().ToTable("LicenseMetered");
			modelBuilder.Entity<LicenseInterval>().ToTable("LicenseInterval");
			modelBuilder.Entity<Session>        ().ToTable("Session");
		}
	}
}