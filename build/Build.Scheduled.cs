using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Utilities.Collections;
using Spectre.Console;

[GitHubActionsWithKeyVault(
    "post-scheduled-cron",
    OnCronSchedule = "10 13,15 * * *",
    InvokedTargets = new[] { nameof(ChooseScheduled) })]
[GitHubActionsWithKeyVault(
    "post-scheduled-push",
    OnPushBranches = new[] { "main" },
    OnPushIncludePaths = new[] { "scheduled/*/*.md" },
    InvokedTargets = new[] { nameof(ChooseScheduled) })]
partial class Build
{
    AbsolutePath ScheduledDirectory => RootDirectory / "scheduled";
    AbsolutePath ScheduledPostedDirectory => ScheduledDirectory / "_posted";

    Target ChooseScheduled => _ => _
        .Triggers(Post)
        .Executes(() =>
        {
            var scheduledDirectory = ScheduledDirectory.GlobFiles($"{DateTime.Today.ToIsoDate()}*/*.md").FirstOrDefault()?.Parent;
            if (scheduledDirectory == null)
                return;

            ScheduledPostedDirectory.CreateDirectory();
            FileSystemTasks.MoveDirectoryToDirectory(scheduledDirectory, ScheduledPostedDirectory);
            scheduledDirectory = ScheduledPostedDirectory / scheduledDirectory.Name;
            CommitAndPush($"Post {scheduledDirectory.Name}", ScheduledDirectory);

            MarkdownFile = scheduledDirectory.GlobFiles("*.md").Single();
            MediaFiles = scheduledDirectory.GlobFiles("*.{png,jpg,jpeg,gif}");
        });

    Target CreateScheduled => _ => _
        .Executes(() =>
        {
            var topic = AnsiConsole.Ask<string>("What's the topic?")
                .Replace(" ", "-").ToLowerInvariant();

            string GetBusyIndicator(DateTime date)
                => new string('*', ScheduledDirectory.GlobDirectories($"{date.ToIsoDate()}-*").Count);

            var nextDays = DateTime.Today.DescendantsAndSelf(x => x.AddDays(1))
                .TakeWhile(x => x.Subtract(TimeSpan.FromDays(7)) < DateTime.Today);

            var day = AnsiConsole.Prompt(
                new SelectionPrompt<DateTime>()
                    .Title("Post on which day?")
                    .HighlightStyle(new Style(Color.Turquoise2))
                    .AddChoices(nextDays)
                    .UseConverter(x => x.ToString("MMM-dd, ddd") + " " + GetBusyIndicator(x)));

            var markdownFile = RootDirectory / "scheduled" / $"{day.ToIsoDate()}-{topic}" / "index.md";
            Assert.False(markdownFile.Exists());
            markdownFile.TouchFile();

            ConfirmEdit(markdownFile);
            CommitAndPush($"Add {markdownFile.Parent.NotNull().Name}", ScheduledDirectory);
        });
}
