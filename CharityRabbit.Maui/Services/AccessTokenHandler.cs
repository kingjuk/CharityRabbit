using System.Net;
using System.Net.Http.Headers;

namespace CharityRabbit.Maui.Services;

/// <summary>Attaches the bearer token to API calls; on a 401, forces one refresh and retries once.</summary>
public class AccessTokenHandler(AuthService auth) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await auth.GetAccessTokenAsync();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized || token is null)
        {
            return response;
        }

        var fresh = await auth.GetAccessTokenAsync(forceRefresh: true);
        if (fresh is null || fresh == token) return response;

        response.Dispose();
        var retry = await CloneAsync(request);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fresh);
        return await base.SendAsync(retry, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsByteArrayAsync();
            var content = new ByteArrayContent(body);
            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            clone.Content = content;
        }
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }
}
