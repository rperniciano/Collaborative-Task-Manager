using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace CollaborativeTaskManager.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class CollaborativeTaskManagerDbContextFactory : IDesignTimeDbContextFactory<CollaborativeTaskManagerDbContext>
{
    public CollaborativeTaskManagerDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        CollaborativeTaskManagerEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<CollaborativeTaskManagerDbContext>()
            .UseSqlite(configuration.GetConnectionString("Default"));
        
        return new CollaborativeTaskManagerDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../CollaborativeTaskManager.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
