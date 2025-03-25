using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql;
using System.IO;

namespace FinanceCalendar
{
    public class FinanceCalendarContext : DbContext
    {
        public FinanceCalendarContext(DbContextOptions<FinanceCalendarContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Event> Events { get; set; }

        public static void RunSqlScript(string connectionString, string scriptPath)
        {
            var script = File.ReadAllText(scriptPath);

            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            using var command = new NpgsqlCommand(script, connection);
            command.ExecuteNonQuery();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
           var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Utc 
                        ? v 
                        : DateTime.SpecifyKind(v, DateTimeKind.Utc), // don't shift anything
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)       // read as UTC
            );


            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                }
            }

            modelBuilder.Entity<User>().Ignore(u => u.Account);

            base.OnModelCreating(modelBuilder);
        }
    }
}