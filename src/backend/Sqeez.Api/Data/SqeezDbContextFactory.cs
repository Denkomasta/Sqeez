using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sqeez.Api.Data;

public class SqeezDbContextFactory : IDesignTimeDbContextFactory<SqeezDbContext>
{
    public SqeezDbContext CreateDbContext(string[] args)
    {
        var envFiles = new[] { ".env", ".env.local" }.Where(File.Exists).ToArray();
        if (envFiles.Length > 0)
        {
            DotNetEnv.Env.LoadMulti(envFiles);
        }

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                               ?? "Host=dummy;Database=dummy;";

        var optionsBuilder = new DbContextOptionsBuilder<SqeezDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new SqeezDbContext(optionsBuilder.Options);
    }
}
