using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sqeez.Api.Data;

/// <summary>
/// Creates a database context for Entity Framework design-time commands such as migration generation.
/// </summary>
/// <remarks>
/// The factory loads local environment files when available and falls back to a dummy PostgreSQL connection string
/// so EF can inspect the model even when runtime configuration is not present.
/// </remarks>
public class SqeezDbContextFactory : IDesignTimeDbContextFactory<SqeezDbContext>
{
    /// <summary>
    /// Builds a design-time <see cref="SqeezDbContext"/> instance for EF tooling.
    /// </summary>
    /// <param name="args">Arguments passed by EF tooling. They are currently not used.</param>
    /// <returns>A configured database context instance.</returns>
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
