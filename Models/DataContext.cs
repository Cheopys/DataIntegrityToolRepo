
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

		public DbSet<Customers>      Customers              { get; set; }
		public DbSet<Subscriptions>  Subscriptions          { get; set; }
		public DbSet<LicenseMetered> LicenseMetered         { get; set; }
		public DbSet<LicenseInterval> LicenseInterval       { get; set; }
        public DbSet<Session>        Session                { get; set; }
        public DbSet<SessionTransition>  SessionTransition  { get; set; }
        public DbSet<Users>          Users                  { get; set; }
        public DbSet<ToolParameters> ToolParameters         { get; set; }
        public DbSet<UsersAwaitingMFA> UsersAwaitingMFA     { get; set; }
 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
			modelBuilder.Entity<Customers>        ().ToTable("Customers");
			modelBuilder.Entity<Subscriptions>    ().ToTable("Subscriptions");
			modelBuilder.Entity<LicenseMetered>   ().ToTable("LicenseMetered");
			modelBuilder.Entity<LicenseInterval>  ().ToTable("LicenseInterval");
			modelBuilder.Entity<Session>          ().ToTable("Session");
            modelBuilder.Entity<SessionTransition>().ToTable("SessionTransition");
            modelBuilder.Entity<Users>            ().ToTable("Users");
            modelBuilder.Entity<ToolParameters>   ().ToTable("ToolParameters");
            modelBuilder.Entity<UsersAwaitingMFA> ().ToTable("UsersAwaitingMFA");
        }
    }
}