using System.Diagnostics.CodeAnalysis;
using CloudMed.Automations.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudMed.Automations.Core.DependencyInjection;

[ExcludeFromCodeCoverage]
public static class OptionsExtensions
{
    public static string GetOptionsName<T>()
    {
        return typeof(T).Name.Replace("Options", "");
    }

    public static Dictionary<string, object> BindConfigOptions(this IServiceCollection services, IConfiguration configuration)
    {
        // bind blobOptions and map the option name and value to opt dictionary
        var options = new List<(string, object)>()
        {
            services.BindOptions<GitOptions>(configuration),
            services.BindOptions<AzureAdOptions>(configuration),
        };
        return options.ToDictionary(kv => kv.Item1, kv => kv.Item2);
    }

    public static (string, T) BindOptions<T>(this IServiceCollection services, IConfiguration configuration) where T : class
    {
        var name = GetOptionsName<T>();
        var opts = configuration.GetSection(name).Get<T>();
        services.AddOptions<T>().BindConfiguration(name).ValidateDataAnnotations();
        return (name, opts);
    }

    public static T GetOptions<T>(this Dictionary<string, object> options) where T : class
    {
        return (T)options[GetOptionsName<T>()];
    }
}
