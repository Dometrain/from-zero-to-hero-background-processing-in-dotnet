using APIProject.Models;
using Microsoft.EntityFrameworkCore;

namespace APIProject
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<BackgroundJob> BackgroundJobs { get; set; }
    }
}
