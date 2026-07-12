using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CharityRabbit.Data;

// Lets `dotnet ef` construct the context at design time without running the web host.
public class CharityDbContextFactory : IDesignTimeDbContextFactory<CharityDbContext>
{
    public CharityDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CHARITY_POSTGRES")
                 ?? "Host=localhost;Port=5432;Database=charityrabbit;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<CharityDbContext>().UseNpgsql(cs).Options;
        return new CharityDbContext(options);
    }
}
