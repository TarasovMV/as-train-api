using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace VrRestApi.Models.Context
{
    public class AdditionalContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionStringBuilder = new SqliteConnectionStringBuilder { DataSource = "DB_add.db" };
            var connectionString = connectionStringBuilder.ToString();
            var connection = new SqliteConnection(connectionString);

            optionsBuilder.UseSqlite(connection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
        }

        public DbSet<Participant> Participants { get; set; }
        public DbSet<ParticipantResult> ParticipantResults { get; set; }
    }
}
