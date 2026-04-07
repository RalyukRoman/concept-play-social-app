using Microsoft.EntityFrameworkCore;

namespace DLL_ConnectionToDatabase
{
    public sealed class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbSet<Concept>       Concept       => Set<Concept>();
        public DbSet<Genre>         Genre         => Set<Genre>();
        public DbSet<UserInfo>      UserInfo      => Set<UserInfo>();
        public DbSet<Comment>       Comment       => Set<Comment>();
        public DbSet<Review>        Review        => Set<Review>();
        public DbSet<ConceptImages> ConceptImages => Set<ConceptImages>();

        public AppDbContext(DbContextOptions<AppDbContext> options) 
            : base(options)
        {
            Database.EnsureCreated();
        }

        public AppDbContext(string connectionString) 
            : base(GetOptions(connectionString))
        {
            Database.EnsureCreated();
        }

        private static DbContextOptions<AppDbContext> GetOptions(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return optionsBuilder.Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserInfo>().HasIndex(x => x.Username).IsUnique();
            modelBuilder.Entity<Genre>()   .HasIndex(x => x.Name)    .IsUnique();
            modelBuilder.Entity<Concept>() .HasIndex(x => x.Title)   .IsUnique();

            modelBuilder.Entity<Review>()
                        .HasOne(r => r.User)
                        .WithMany(u => u.Reviews)
                        .HasForeignKey(r => r.UserId)
                        .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<Concept>()
                .HasMany(c => c.Genres)
                .WithMany(g => g.Concepts);
        }
    }
}
