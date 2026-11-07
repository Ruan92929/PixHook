using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PixHook.Infrastructure.Persistence;

/// <summary>Usado por <c>dotnet ef migrations add</c>.</summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PixHookDbContext>
{
    public PixHookDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PixHookDbContext>()
            .UseSqlite("Data Source=pixhook.db")
            .Options;
        return new PixHookDbContext(options);
    }
}
