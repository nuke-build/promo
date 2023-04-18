using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Utilities;
using static Nuke.Common.Tools.Git.GitTasks;

partial class Build
{
    GitHubActions GitHubActions => GitHubActions.Instance;

    string CommitterName => GitHubActions.Actor;
    string CommitterEmail => "actions@github.com";

    void CommitAndPush(string message, AbsolutePath directory = null, string token = null)
    {
        if (GitHubActions != null)
        {
            var remote = $"https://oauth:{token ?? GitHubActions.Token}@github.com/{Repository.GetGitHubOwner()}/{Repository.GetGitHubName()}";
            Git($"remote set-url origin {remote}");
            Git($"config user.name {CommitterName}");
            Git($"config user.email {CommitterEmail}");
            Git($"config --remove-section http.{"https://github.com/".DoubleQuote()}");
        }

        if (directory != null)
            Git($"add {directory}");

        Git($"commit -m {message} --allow-empty");
        Git($"push origin HEAD:{Repository.Branch}");
    }
}
