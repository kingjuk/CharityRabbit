namespace CharityRabbit.Platform;

public class WebAppEnvironment(IWebHostEnvironment env) : IAppEnvironment
{
    public bool IsDevelopment() => env.IsDevelopment();
}
