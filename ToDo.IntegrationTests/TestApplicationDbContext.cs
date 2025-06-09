using Microsoft.EntityFrameworkCore;
using ToDo.Models;

namespace ToDo.IntegrationTests
{
    public class TestApplicationDbContext : DbContext
    {
        public TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Todo> Todos { get; set; }
        public DbSet<JournalEntry> Journals { get; set; }
        public DbSet<User> Users { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Todo>().ToTable("Todos");
            modelBuilder.Entity<JournalEntry>().ToTable("Journals");
            modelBuilder.Entity<User>().ToTable("Users");
        }
    }
} 