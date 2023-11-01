using CloudMed.Automations.Core.Interfaces;
using CloudMed.Automations.Core.Options;
using Microsoft.Azure.Pipelines.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CloudMed.Automations.Core.Services;

public class PipelineService : IPipelineService
{
    private readonly IPipelineClient pipelineClient;
    private readonly GitOptions gitOptions;
    private readonly ILogger<PipelineService> logger;

    public PipelineService(IPipelineClient pipelineClient, IOptions<GitOptions> gitOptions, ILogger<PipelineService> logger)
    {
        this.pipelineClient = pipelineClient;
        this.gitOptions = gitOptions.Value;
        this.logger = logger;
    }

    public async Task<List<Run>> RunCloudMedPipelines(bool initial)
    {
        try
        {
            var project = this.gitOptions.ProjectName;
            var pipelines = await this.pipelineClient.ListPipelines(project).ConfigureAwait(false);

            var cmPipelineNames = this.gitOptions.PipelineProjectNames
                .Where(x => x.initial == initial)
                .Select(x => x.name)
                .ToHashSet();

            var cmPipelinesToRun = pipelines.Where(x => cmPipelineNames.Contains(x.Name)).ToList();
            var runs = new List<Run>();

            foreach (var pipeline in cmPipelinesToRun)
            {
                cmPipelineNames.Remove(pipeline.Name);
                var parameters = new RunPipelineParameters();
                var run = await pipelineClient.RunPipeline(project, pipeline.Id).ConfigureAwait(false);
                this.logger.LogDebug($"{run.Name} - {run.Id}");
                runs.Add(run);
            }

            foreach (var missingCmPipeline in cmPipelineNames)
            {
                this.logger.LogError($"{missingCmPipeline} - NotFound");
            }

            return runs;
        }
        catch (Exception ex)
        {
            this.logger.LogError($"{ex}");
            throw;
        }
    }
}
