using IdentityModel.Client;
using IdentityModel.OidcClient.Browser;

namespace CharityRabbit.Maui.Services;

/// <summary>Bridges IdentityModel.OidcClient to the platform browser
/// (ASWebAuthenticationSession on iOS) via MAUI's WebAuthenticator.</summary>
public class WebAuthenticatorBrowser : IdentityModel.OidcClient.Browser.IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new WebAuthenticatorOptions
                {
                    Url = new Uri(options.StartUrl),
                    CallbackUrl = new Uri(options.EndUrl),
                });

            // Rebuild the callback URL the OIDC client expects to parse.
            var url = new RequestUrl(options.EndUrl).Create(new Parameters(result.Properties));
            return new BrowserResult { Response = url, ResultType = BrowserResultType.Success };
        }
        catch (TaskCanceledException)
        {
            return new BrowserResult { ResultType = BrowserResultType.UserCancel };
        }
        catch (Exception ex)
        {
            return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.Message };
        }
    }
}
