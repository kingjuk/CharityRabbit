using Microsoft.JSInterop;

namespace CharityRabbit.Platform;

/// <summary>Native share sheet (mobile browsers) or clipboard fallback, via the
/// charityRabbitShare helper defined inline in App.razor.</summary>
public class WebShareProvider(IJSRuntime js) : IShareProvider
{
    public async Task<ShareOutcome> ShareAsync(string title, string text, string url)
    {
        try
        {
            var result = await js.InvokeAsync<string>("charityRabbitShare", title, text, url);
            return result == "copied" ? ShareOutcome.Copied : ShareOutcome.Shared;
        }
        catch (JSException)
        {
            return ShareOutcome.Failed;
        }
    }
}
