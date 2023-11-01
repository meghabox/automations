# Get Started

- Follow instuctions here to create a Personal Access Token ([PAT](https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows))
- Copy and paste you PAT into the `CloudMed.Automations\appsettings.json` file under `AzureAd:PersonalAccessToken`.

# What is run
- When you run the `CloudMed.Automations` project it will run the console app. The following tasks are performed:
1. Run Dev build pipeline for code styles
2. Create PRs for all repos that have changes. This will link work items and set the newly created PR to auto complete. Multiple merge bases will be logged and should be handled manually.
3. Run Dev build pipeline for the remaining dev pipelines that remain in the GitOptions (!initial).

