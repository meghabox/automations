using CloudMed.Automations.Core.Interfaces;
using CloudMed.Automations.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Collections.Concurrent;

namespace CloudMed.Automations.Core.Services;

public class VssConnectionFactory : IDisposable, IVssConnectionFactory
{
    private readonly IDictionary<string, VssConnection> connections = new ConcurrentDictionary<string, VssConnection>();
    private readonly AzureAdOptions azureAdOptions;
    private bool disposedValue;

    public VssConnectionFactory(IOptions<AzureAdOptions> azureAdOptions)
    {
        this.azureAdOptions = azureAdOptions.Value;
    }

    public VssConnection CreateVssConnection(string collectionUri)
    {
        if (connections.TryGetValue(collectionUri, out var connection))
        {
            return connection;
        }

        connection = new VssConnection(new Uri(collectionUri), new VssBasicCredential("", this.azureAdOptions.PersonalAccessToken));

        connections.Add(collectionUri, connection);
        return connection;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            connections.Values.ForEach(conn => conn?.Dispose());
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
