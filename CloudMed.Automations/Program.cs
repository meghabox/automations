using CloudMed.Automations.Core.DependencyInjection;
using CloudMed.Automations.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudMed.Automations;

internal class Program
{
    static async Task Main(string[] args)
    {
        var services = Extensions.ConfigureServices();
        var prService = services.GetRequiredService<IPullRequestService>();
        var pipelineService = services.GetRequiredService<IPipelineService>();
        
        await pipelineService.RunCloudMedPipelines(true);

        await prService.CreateDevToMainPullRequestsAsync().ConfigureAwait(false);

        await pipelineService.RunCloudMedPipelines(false);
    }
}

public static class Extensions
{
    public static IServiceProvider ConfigureServices()
    {
        IServiceCollection services = new ServiceCollection();
        var config = BuildConfiguration();

        services.BindConfigOptions(config);

        services.AddSingleton<IConfiguration>((s) => config);

        DependencyInjection.ConfigureServices(services);
        return services.BuildServiceProvider();
    }

    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
    }
}