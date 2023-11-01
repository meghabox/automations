using CloudMed.Automations.Core.Models;

namespace CloudMed.Automations.Core.Interfaces;
public interface IWorkItemClient
{
    Task<WorkItemUpdateResponse> UpdateWorkItemAsync<T>(T document, int id);
}