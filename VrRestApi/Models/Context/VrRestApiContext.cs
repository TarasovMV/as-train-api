using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace VrRestApi.Models.Context
{
    public class VrRestApiContext: DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "DB.db" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);

            optionsBuilder.UseSqlite(connection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserCategory>().HasMany(c => c.Users).WithOne(u => u.Category).OnDelete(DeleteBehavior.ClientSetNull);
        }

        public DbSet<FileModel> Files { get; set; }

        public DbSet<User> Users { get; set; }
        public DbSet<UserCategory> UserCategories { get; set; }

        //public DbSet<TestingSetSetupConnect> TestingSetSetupConnects { get; set; }
        //public DbSet<TestingSetup> TestingSetups { get; set; }
        public DbSet<TestingSet> TestingSets { get; set; }
        //public DbSet<Testing> ResultTestings { get; set; }
        public DbSet<Testing> Testings { get; set; }
        public DbSet<TestingStage> TestingStages { get; set; }
        public DbSet<TestingQuestion> TestingQuestions { get; set; }
        public DbSet<TestingAnswer> TestingAnswers { get; set; }
        public DbSet<CompetitionResult> Results { get; set; }
        public DbSet<TestingScore> Scores { get; set; }
    }
}
