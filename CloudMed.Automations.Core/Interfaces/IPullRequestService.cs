using Microsoft.TeamFoundation.SourceControl.WebApi;
namespace CloudMed.Automations.Core.Interfaces;
public interface IPullRequestService
{
    Task<Dictionary<string, (bool success, GitPullRequest pullRequest)>> CreateDevToMainPullRequestsAsync();
}