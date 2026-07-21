namespace CharityRabbit.Platform;

/// <summary>
/// Host-environment facts components need without referencing ASP.NET hosting types
/// (which don't exist in the MAUI host). Method (not property) so existing
/// IWebHostEnvironment.IsDevelopment() call sites read the same.
/// </summary>
public interface IAppEnvironment
{
    bool IsDevelopment();
}
