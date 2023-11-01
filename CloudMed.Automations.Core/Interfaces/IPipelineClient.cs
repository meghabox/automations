using Microsoft.Azure.Pipelines.WebApi;

namespace CloudMed.Automations.Core.Interfaces
{
    public interface IPipelineClient
    {
        Task<Run> RunPipeline(string project, int pipelineId);
        Task<Pipeline[]> ListPipelines(string project);
    }
}