using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CloseExpAISolution.Infrastructure.Context;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var apiConfigPath = ResolveApiConfigPath(currentDir);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiConfigPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, b =>
            b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static string ResolveApiConfigPath(string currentDir)
    {
        var candidates = new[]
        {
            currentDir,
            Path.Combine(currentDir, "src", "CloseExpAISolution.API"),
            Path.GetFullPath(Path.Combine(currentDir, "..", "CloseExpAISolution.API")),
            Path.GetFullPath(Path.Combine(currentDir, "..", "..", "CloseExpAISolution.API"))
        };

        foreach (var path in candidates)
        {
            if (File.Exists(Path.Combine(path, "appsettings.json")))
            {
                return path;
            }
        }

        throw new DirectoryNotFoundException("Could not find CloseExpAISolution.API appsettings.json for design-time DbContext creation.");
    }
}
