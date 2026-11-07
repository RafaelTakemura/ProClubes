using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ProClubesApi.Data;

public class ProClubesDbContextFactory : IDesignTimeDbContextFactory<ProClubesDbContext>
{
    public ProClubesDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = Environment.GetEnvironmentVariable("PROCLUBES_MYSQL_CONNECTION")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Defina a connection string em 'appsettings.Development.json' (ConnectionStrings:DefaultConnection) ou na variável de ambiente 'PROCLUBES_MYSQL_CONNECTION'.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ProClubesDbContext>();
        optionsBuilder.UseMySQL(connectionString);
        return new ProClubesDbContext(optionsBuilder.Options);
    }
}
