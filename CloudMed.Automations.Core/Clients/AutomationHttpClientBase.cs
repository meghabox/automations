using Microsoft.VisualStudio.Services.Common.Diagnostics;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace CloudMed.Automations.Core.Clients;

public class AutomationHttpClientBase : VssHttpClientBase
{
    public AutomationHttpClientBase(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
    {
    }

    public AutomationHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings) : base(baseUrl, credentials, settings)
    {
    }

    public AutomationHttpClientBase(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers) : base(baseUrl, credentials, handlers)
    {
    }

    public AutomationHttpClientBase(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler) : base(baseUrl, pipeline, disposeHandler)
    {
    }

    public AutomationHttpClientBase(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers) : base(baseUrl, credentials, settings, handlers)
    {
    }

    protected async Task<T> SendRequestAsync<T>(HttpMethod method, string relativeUri, string version, HttpContent content = null, IEnumerable<KeyValuePair<string, string>> queryParameters = null)
    {
        using (VssTraceActivity.GetOrCreate().EnterCorrelationScope())
        {
            using HttpRequestMessage requestMessage = CreateBatchRequestMessage(method, relativeUri, version, content);
            return await SendRequestAsync<T>(requestMessage).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    protected HttpRequestMessage CreateBatchRequestMessage(HttpMethod method, string relativeUri, string version, HttpContent content = null, string mediaType = "application/json")
    {
        var uri = VssHttpUriUtility.ConcatUri(BaseAddress, relativeUri);
        var httpRequestMessage = new HttpRequestMessage(method, uri.AbsoluteUri);
        var mediaTypeWithQualityHeaderValue = new MediaTypeWithQualityHeaderValue(mediaType);

        httpRequestMessage.Headers.Add("accept", $"application/json;api-version={version};excludeUrls=true;enumsAsNumbers=true;msDateFormat=true;noArrayWrap=true");

        if (content != null)
        {
            httpRequestMessage.Content = content;
            if (httpRequestMessage.Content!.Headers.ContentType != null && !httpRequestMessage.Content!.Headers.ContentType!.Parameters.Any((p) => p.Name.Equals("api-version")))
            {
                httpRequestMessage.Content!.Headers.ContentType!.Parameters.Add(new NameValueHeaderValue("api-version", version.ToString()));
            }
        }

        return httpRequestMessage;
    }

    protected async Task<T> SendRequestAsync<T>(HttpRequestMessage message, object userState = null, CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await SendAsync(message, userState, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        return await ReadContentAsync<T>(response, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
    }

    protected async Task<T> ReadContentAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonConvert.DeserializeObject<T>(json);
    }
}
