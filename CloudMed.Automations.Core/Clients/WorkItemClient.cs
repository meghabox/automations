using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using CloudMed.Automations.Core.Interfaces;
using CloudMed.Automations.Core.Models;

namespace CloudMed.Automations.Core.Clients;

public class WorkItemClient : AutomationHttpClientBase, IWorkItemClient
{
    public WorkItemClient(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
    {
    }

    public virtual async Task<WorkItemUpdateResponse> UpdateWorkItemAsync<T>(T document, int id)
    {
        var method = new HttpMethod("PATCH");
        var relativeUri = "/_apis/wit/$batch";
        var content = new ObjectContent<dynamic>(document, new VssJsonMediaTypeFormatter(bypassSafeArrayWrapping: true), "application/json");
        var version = "4.0-preview";
        var responseList = await SendRequestAsync<WorkItemUpdateResponse[]>(method, relativeUri, version, content).ConfigureAwait(false);
        return responseList.FirstOrDefault();
    }
}
