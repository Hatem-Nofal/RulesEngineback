using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace RulesEngine.EntityFrameworkCore;

/* This class is needed for EF Core console commands
 * (like Add-Migration and Update-Database commands) */
public class RulesEngineDbContextFactory : IDesignTimeDbContextFactory<RulesEngineDbContext>
{
    public RulesEngineDbContext CreateDbContext(string[] args)
    {
        var configuration = BuildConfiguration();
        
        RulesEngineEfCoreEntityExtensionMappings.Configure();

        var builder = new DbContextOptionsBuilder<RulesEngineDbContext>()
            .UseSqlServer(configuration.GetConnectionString("Default"));
        
        return new RulesEngineDbContext(builder.Options);
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../RulesEngine.DbMigrator/"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}
