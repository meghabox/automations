using CloudMed.Automations.Core.Interfaces;
using CloudMed.Automations.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using WorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;
using Microsoft.VisualStudio.Services.Common;

namespace CloudMed.Automations.Core.Services;
public class PullRequestService : IPullRequestService
{
    private readonly IVssConnectionFactory vssConnectionFactory;
    private readonly GitOptions gitOptions;
    private readonly IWorkItemClient workItemClient;
    private readonly ILogger logger;
    const string PRAlreadyExists = "TF401179";

    public PullRequestService(IVssConnectionFactory vssConnectionFactory, IOptions<GitOptions> gitOptions, IWorkItemClient workItemClient, ILogger<PullRequestService> logger)
    {
        this.vssConnectionFactory = vssConnectionFactory;
        this.gitOptions = gitOptions.Value;
        this.workItemClient = workItemClient;
        this.logger = logger;
    }

    public string PadRepoName(int width, string repoName)
    {
        return repoName.PadRight(width, ' ');
    }

    public async Task<Dictionary<string, (bool success, GitPullRequest pullRequest)>> CreateDevToMainPullRequestsAsync()
    {
        var results = new Dictionary<string, (bool success, GitPullRequest pullRequest)>();
        try
        {
            var connection = this.vssConnectionFactory.CreateVssConnection(this.gitOptions.CollectionUri);
            var projectName = this.gitOptions.ProjectName;
            var gitClient = connection.GetClient<GitHttpClient>();
            var maxRepoNameLength = this.gitOptions.RepositoryNames.MaxBy(x => x.Length)?.Length ?? 0;

            foreach (var repoName in this.gitOptions.RepositoryNames)
            {
                try
                {
                    this.logger.LogDebug($"{PadRepoName(maxRepoNameLength, repoName)} - starting");
                    var (devBranch, mainBranch) = await GetBranchesForDevToMainAsync(gitClient, projectName, repoName).ConfigureAwait(false);

                    if (devBranch == null || mainBranch == null)
                    {
                        this.logger.LogError($"{PadRepoName(maxRepoNameLength, repoName)} - no dev or main branch found");
                        results.Add(repoName, (false, null));
                        continue;
                    }

                    var gitRepository = await gitClient.GetRepositoryAsync(projectName, repoName).ConfigureAwait(false);

                    var (prStatus, pullRequestToCreate) = await GetNewPullRequestWithWorkItemsAsync(gitClient, devBranch, mainBranch, projectName, gitRepository.Id).ConfigureAwait(false);
                    
                    if (pullRequestToCreate == null)
                    {
                        this.logger.LogWarning($"{PadRepoName(maxRepoNameLength, repoName)} - {prStatus}");
                        results.Add(repoName, (false, null));
                        continue;
                    }

                    var createdPR = await CreateAutoCompletePullRequestAsync(gitClient, pullRequestToCreate, gitRepository, projectName).ConfigureAwait(false);

                    if (createdPR.HasMultipleMergeBases)
                    {
                        this.logger.LogError($"{PadRepoName(maxRepoNameLength, repoName)} - {nameof(createdPR.HasMultipleMergeBases)}");
                    }

                    await UpdateWorkItemAddCommitLinkAsync(workItemClient, createdPR, pullRequestToCreate.WorkItemRefs).ConfigureAwait(false);

                    results.Add(repoName, (true, createdPR));
                }
                catch (VssException vssEx) when(vssEx.Message.StartsWith(PRAlreadyExists))
                {
                    this.logger.LogWarning($"{PadRepoName(maxRepoNameLength, repoName)} - PR Already Exists");
                }
                catch (Exception ex)
                {
                    this.logger.LogError($"{PadRepoName(maxRepoNameLength, repoName)} - ERROR creating PR {ex}");
                    results.Add(repoName, (false, null));
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            this.logger.LogError($"{ex}");
            throw;
        }
    }

    private async Task<(PRCreateStatus createStatus, GitPullRequest prToCreate)> GetNewPullRequestWithWorkItemsAsync(
        GitHttpClient gitClient, GitBranchStats devBranch, GitBranchStats mainBranch, string projectName, Guid repoId)
    {
        var (prStatus, linkedWorkItems) = await TryCreatePR(gitClient, devBranch.Name, mainBranch.Name, repoId, projectName).ConfigureAwait(false);

        if (prStatus != PRCreateStatus.PRNeeded)
        {
            return (prStatus, null);
        }

        return (prStatus, new GitPullRequest
        {
            SourceRefName = BranchPath(devBranch.Name),
            TargetRefName = BranchPath(mainBranch.Name),
            Title = $"{devBranch.Name} -> {mainBranch.Name}",
            Description = $"Automated pull request. Dev to Main.",
            WorkItemRefs = linkedWorkItems,
            CompletionOptions = new GitPullRequestCompletionOptions()
            {
                BypassPolicy = true,
                DeleteSourceBranch = false,
                BypassReason = "AutoMerge Dev to Main",
                MergeStrategy = GitPullRequestMergeStrategy.NoFastForward
            }
        });
    }

    private static async Task<(GitBranchStats? devBranch, GitBranchStats? mainBranch)> GetBranchesForDevToMainAsync(
        GitHttpClient gitClient, string projectName, string repository)
    {
        var branchStats = await gitClient.GetBranchesAsync(projectName, repository).ConfigureAwait(false);

        GitBranchStats devBranch = null;
        GitBranchStats mainBranch = null;

        bool IsBranchNameEqual(GitBranchStats branchStat, string branchName)
        {
            return branchStat.Name.Equals(branchName, StringComparison.CurrentCultureIgnoreCase);
        }

        foreach (GitBranchStats branchStat in branchStats)
        {
            if (IsBranchNameEqual(branchStat, "dev"))
            {
                devBranch = branchStat;
            }
            else if (IsBranchNameEqual(branchStat, "main") || IsBranchNameEqual(branchStat, "master"))
            {
                mainBranch = branchStat;
            }

            if (devBranch != null && mainBranch != null)
            {
                break;
            }
        }

        return (devBranch, mainBranch);
    }

    public async Task<GitPullRequest> CreateAutoCompletePullRequestAsync(
        GitHttpClient gitClient, GitPullRequest pullRequestToCreate, GitRepository gitRepository, string projectName)
    {
        var createdPR = await gitClient.CreatePullRequestAsync(pullRequestToCreate, projectName, gitRepository.Id).ConfigureAwait(false);

        try
        {
            // try to set auto complete, but if it fails to update to auto that is ok too
            var a = createdPR.WorkItemRefs;
            var autoCompletePR = new GitPullRequest
            {
                AutoCompleteSetBy = createdPR.CreatedBy,
                WorkItemRefs = pullRequestToCreate.WorkItemRefs,
                CompletionOptions = new GitPullRequestCompletionOptions
                {
                    SquashMerge = false,
                    DeleteSourceBranch = false,
                    MergeCommitMessage = "auto complete"
                }
            };

            createdPR = await gitClient.UpdatePullRequestAsync(autoCompletePR, createdPR.Repository.Id, createdPR.PullRequestId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError($"{gitRepository.Name} - {ex}");
        }

        // at least we have a non-auto complete PR
        return createdPR;
    }

    public async Task<dynamic> UpdateWorkItemAddCommitLinkAsync(
        IWorkItemClient workItemClient, GitPullRequest createdPR, IEnumerable<ResourceRef> workItemsToUpdate)
    {
        if (workItemsToUpdate == null || !workItemsToUpdate.Any())
        {
            return Array.Empty<WorkItem>();
        }

        var pullRequestUrl = $@"vstfs:///Git/PullRequestId/{createdPR.Repository.ProjectReference.Id}%2F{createdPR.Repository.Id}%2F{createdPR.PullRequestId}";

        var patchOpp = new JsonPatchOperation {
            Operation = Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Add,
            Path = "/relations/-",
            Value = new {
                rel = "ArtifactLink",
                url = pullRequestUrl,
                attributes = new { name = "Pull Request" }
            }
        };

        var version = "4.0-preview";
        var workItemLinkingTasks = workItemsToUpdate
            .DistinctBy(workItem => workItem.Id)
            .Select(workItem =>
            {
                var bulkPatch = new object[] {
                    new {
                        method = "PATCH",
                        uri = $"/_apis/wit/workItems/{workItem.Id}?api-version={version}",
                        headers = new Dictionary<string, string>() { { "Content-Type", "application/json-patch+json" } },
                        body = new JsonPatchDocument { patchOpp }
                    }
                };

                var json = JsonConvert.SerializeObject(bulkPatch);

                return workItemClient.UpdateWorkItemAsync(bulkPatch, int.Parse(workItem.Id));
            });
        var workItems = await Task.WhenAll(workItemLinkingTasks.Take(1)).ConfigureAwait(false);
        return workItems;
    }

    public async Task<(PRCreateStatus createStatus, ResourceRef[] linkedWorkItems)> TryCreatePR(
        GitHttpClient gitClient, string devBranch,string mainBranch, Guid repoId, string projectId)
    {
        if (await IsDevToMainPrExists(gitClient, devBranch, mainBranch, repoId, projectId).ConfigureAwait(false)) 
        {
            return (PRCreateStatus.PRExists, null);
        }
        
        var devSearchCriteria = new GitPullRequestSearchCriteria() 
        {
            TargetRefName = BranchPath(devBranch),
            Status = PullRequestStatus.Completed
        };

        var mainSearchCriteria = new GitPullRequestSearchCriteria()
        {
            SourceRefName = BranchPath(devBranch),
            TargetRefName = BranchPath(mainBranch),
            Status = PullRequestStatus.Completed
        };

        List<GitPullRequest> devPullRequests = await gitClient.GetPullRequestsAsync(projectId, repoId, devSearchCriteria).ConfigureAwait(false);
        List<GitPullRequest> mainPullRequests = await gitClient.GetPullRequestsAsync(projectId, repoId, mainSearchCriteria, top: 1).ConfigureAwait(false);

        var lastMainPrCloseDate = mainPullRequests.OrderBy(pr => pr.ClosedDate).FirstOrDefault()?.ClosedDate;
        var prsSinceLastDevToMain = devPullRequests.Where(p => p.ClosedDate > lastMainPrCloseDate).ToList();
        if (!prsSinceLastDevToMain.Any())
        {
            return (PRCreateStatus.NoChanges, null);
        }

        var tasks = prsSinceLastDevToMain.Select(pr => gitClient.GetPullRequestWorkItemRefsAsync(repoId, pr.PullRequestId));
        var workItemsResults = await Task.WhenAll(tasks).ConfigureAwait(false);
        var workItems = workItemsResults.SelectMany(items => items.Select(workItem => new ResourceRef() { Id = workItem.Id, Url = workItem.Url }));

        return (PRCreateStatus.PRNeeded, workItems?.ToArray() ?? Array.Empty<ResourceRef>());
    }

    protected async Task<bool> IsDevToMainPrExists(
        GitHttpClient gitClient, string devBranch, string mainBranch, Guid repoId, string projectId)
    {
        var activeDevToMainSearch = new GitPullRequestSearchCriteria()
        {
            SourceRefName = BranchPath(devBranch),
            TargetRefName = BranchPath(mainBranch),
            Status = PullRequestStatus.Active
        };

        var isAnActiveDevToMain = (await gitClient.GetPullRequestsAsync(projectId, repoId, activeDevToMainSearch).ConfigureAwait(false)).Any();
        return isAnActiveDevToMain;
    }

    public static string BranchPath(string branchName) => $"refs/heads/{branchName}";
}

public enum PRCreateStatus
{
    PRExists,
    NoChanges,
    PRNeeded
}