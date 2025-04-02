
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using ProxChat.Schema;
using System.Data;
using EFCore.NamingConventions;

namespace ProxChat.Db
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
			options.UseNpgsql("Host=proxchat.cb4opfsjssoq.us-west-1.rds.amazonaws.com;Port=5432; Database=proxchat;Username=postgres;Password=XPJn7sMReKm0HA6ttB2F ;Include Error Detail=true");
        }

		public DbSet<Users>              Users              { get; set; }
		public DbSet<Chats>              Chats              { get; set; }
        public DbSet<ChatUsers>          ChatUsers          { get; set; }
        public DbSet<ChatRadii>			 ChatRadii			{ get; set; }
		public DbSet<ChatMessages>       ChatMessages       { get; set; }
		public DbSet<UsersOnline>        UsersOnline        { get; set; } 
		public DbSet<DirectMessages>     DirectMessages     { get; set; }
		public DbSet<UserProfiles>		 UserProfiles		{ get; set; }
		public DbSet<PrivacySettings>    PrivacySettings    { get; set; }
        public DbSet<RelationshipStatus> RelationshipStatus { get; set; }
        public DbSet<UserInterests>      UserInterests      { get; set; }
        public DbSet<InterestNames>      InterestNames      { get; set; }
        public DbSet<ProfileInterests>   ProfileInterests   { get; set; }
		public DbSet<UserFriends>        UserFriends  { get; set; }
		public DbSet<FriendRequests>     FriendRequests     { get; set; }
		public DbSet<UserRatings>        UserRatings        { get; set; }
        public DbSet<UserRegistering>    UserRegistering    { get; set; }
		public DbSet<UserBlocks>         UserBlocks         { get; set; }
        public DbSet<LoggingTable>       LoggingTable       { get; set; }
        public DbSet<ApplicationSettings> ApplicationSettings { get; set; }
		public DbSet<ApplicationParameters> ApplicationParameters { get; set; }
		public DbSet<ApplicationText>    ApplicationText    { get; set; }
		public DbSet<Locales>            Locales            { get; set; }
		public DbSet<ForbiddenWords>     ForbiddenWords     { get; set; }
		public DbSet<TranslationOptOut>  TranslationOptOuts { get; set; }
		public DbSet<ChatUserTranslate>  ChatUserTranslate  { get; set; }
		public DbSet<ImageRejectionLog>  ImageRejectionLog  { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
			modelBuilder.Entity<Users>              ().ToTable("Users");
			modelBuilder.Entity<Chats>              ().ToTable("Chats");
			modelBuilder.Entity<ChatUsers>          ().ToTable("ChatUsers");
            modelBuilder.Entity<ChatMessages>       ().ToTable("ChatMessages");
            modelBuilder.Entity<ChatRadii>			().ToTable("ChatRadii");
            modelBuilder.Entity<FriendRequests>     ().ToTable("FriendRequests");
            modelBuilder.Entity<UserFriends>        ().ToTable("UserFriends");
			modelBuilder.Entity<UserRatings>        ().ToTable("UserRatings");
			modelBuilder.Entity<UserRegistering>    ().ToTable("UserRegistering");
			modelBuilder.Entity<UserBlocks>         ().ToTable("UserBlocks");
			modelBuilder.Entity<UsersOnline>        ().ToTable("UsersOnline");
			modelBuilder.Entity<UserProfiles>		().ToTable("UserProfiles");
			modelBuilder.Entity<PrivacySettings>    ().ToTable("PrivacySettings");      
			modelBuilder.Entity<RelationshipStatus> ().ToTable("RelationshipStatus");
			modelBuilder.Entity<UserInterests>      ().ToTable("UserInterests");
			modelBuilder.Entity<InterestNames>      ().ToTable("InterestNames");
			modelBuilder.Entity<ProfileInterests>   ().ToTable("ProfileInterests");
            modelBuilder.Entity<LoggingTable>       ().ToTable("LoggingTable");
			modelBuilder.Entity<ApplicationSettings>().ToTable("ApplicationSettings");
			modelBuilder.Entity<ApplicationParameters>().ToTable("ApplicationParameters").HasNoKey();
			modelBuilder.Entity<ApplicationText>	().ToTable("ApplicationText");
			modelBuilder.Entity<Locales>            ().ToTable("Locales");
			modelBuilder.Entity<ForbiddenWords>     ().ToTable("ForbiddenWords");
			modelBuilder.Entity<TranslationOptOut>  ().ToTable("TranslationOptOut");
			modelBuilder.Entity<ChatUserTranslate> ().ToTable("ChatUserTranslate");
			modelBuilder.Entity<ImageRejectionLog>().ToTable("ImageRejectionLog");
		}
	}
}