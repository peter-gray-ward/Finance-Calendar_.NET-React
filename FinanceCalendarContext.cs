using Microsoft.EntityFrameworkCore;
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
    }
    
}