using APIProject.Models;
using Microsoft.EntityFrameworkCore;

namespace APIProject
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EmailNotification>()
                .HasIndex(n => new { n.DocumentId, n.Type })
                .IsUnique();
        }
        public DbSet<BackgroundJob> BackgroundJobs { get; set; }
        public DbSet<EmailNotification> EmailNotifications { get; set; }
    }
}
