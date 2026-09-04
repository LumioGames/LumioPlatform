using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lumio.Platform.Data;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PLATFORM_DB_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("PLATFORM_DB_CONNECTION_STRING is required for design-time database operations.");
        }

        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connectionString).Options;
        return new PlatformDbContext(options);
    }
}
