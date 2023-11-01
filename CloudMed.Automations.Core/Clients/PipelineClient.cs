using CloudMed.Automations.Core.Interfaces;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace CloudMed.Automations.Core.Clients;

public class PipelineClient : AutomationHttpClientBase, IPipelineClient
{
    public PipelineClient(Uri baseUrl, VssCredentials credentials) : base(baseUrl, credentials)
    {
    }

    public virtual async Task<Run> RunPipeline(string project, int pipelineId)
    {
        var method = new HttpMethod("POST");
        var version = "7.0";
        var relativeUri = $"/{project}/_apis/pipelines/{pipelineId}/runs?api-version={version}";
        var content = new ObjectContent<dynamic>(new { }, new VssJsonMediaTypeFormatter(bypassSafeArrayWrapping: true), "application/json");
        return await SendRequestAsync<Run>(method, relativeUri, version, content).ConfigureAwait(false);
    }

    public virtual async Task<Pipeline[]> ListPipelines(string project)
    {
        var method = new HttpMethod("GET");
        var version = "7.0";
        var relativeUri = $"/{project}/_apis/pipelines?api-version={version}";
        return await SendRequestAsync<Pipeline[]>(method, relativeUri, version).ConfigureAwait(false);
    }
}