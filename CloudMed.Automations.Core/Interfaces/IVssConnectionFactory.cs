using Microsoft.VisualStudio.Services.WebApi;

namespace CloudMed.Automations.Core.Interfaces
{
    public interface IVssConnectionFactory
    {
        VssConnection CreateVssConnection(string repositoryUri);
        void Dispose();
    }
}