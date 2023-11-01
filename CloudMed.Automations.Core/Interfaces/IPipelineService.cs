using Microsoft.Azure.Pipelines.WebApi;

namespace CloudMed.Automations.Core.Interfaces
{
    public interface IPipelineService
    {
        Task<List<Run>> RunCloudMedPipelines(bool initial);
    }
}