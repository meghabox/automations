using CloudMed.Automations.Core.Clients;
using CloudMed.Automations.Core.Interfaces;
using CloudMed.Automations.Core.Options;
using CloudMed.Automations.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using System.IO.Compression;

namespace CloudMed.Automations.Core.DependencyInjection;

public static class DependencyInjection
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddSingleton<IVssConnectionFactory, VssConnectionFactory>();
        services.AddSingleton<IPullRequestService, PullRequestService>();
        services.AddSingleton<IPipelineClient, PipelineClient>();
        services.AddSingleton<IPipelineService, PipelineService>();
        
        services.AddLogging(logging => logging.AddConsole());

        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<GzipCompressionProvider>();
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        services.AddSingleton<IWorkItemClient, WorkItemClient>((sp) =>
        {
            var gitOptions = sp.GetRequiredService<IOptions<GitOptions>>();
            var client = new WorkItemClient(new Uri(gitOptions.Value.CollectionUri), sp.GetVssBasicCredential());
            return client;
        });

        services.AddSingleton<IPipelineClient, PipelineClient>((sp) =>
        {
            var gitOptions = sp.GetRequiredService<IOptions<GitOptions>>();
            var client = new PipelineClient(new Uri(gitOptions.Value.CollectionUri), sp.GetVssBasicCredential());
            return client;
        });
    }

    private static VssBasicCredential GetVssBasicCredential(this IServiceProvider sp)
    {
        var adOptions = sp.GetRequiredService<IOptions<AzureAdOptions>>();
        var vssBasicCredential = new VssBasicCredential("", adOptions.Value.PersonalAccessToken);
        return vssBasicCredential;
    }
}
