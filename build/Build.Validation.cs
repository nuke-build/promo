using System;
using System.Linq;
using NJsonSchema;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using Serilog;

[GitHubActions(
    "validation",
    GitHubActionsImage.UbuntuLatest,
    OnPushIncludePaths = new [] { "scheduled/**", "tips/**" },
    InvokedTargets = new[] { nameof(Validate) })]
partial class Build
{
    AbsolutePath SchemaFile => RootDirectory / "tips" / "schema.json";

    public const int MaximumTweetLength = 280;
    public const int MaximumMediaSize = 2 * 1024 * 1024;

    Target GenerateSchema => _ => _
        .Executes(() =>
        {
            var schema = JsonSchema.FromType<Post>();
            SchemaFile.WriteAllText(schema.ToJson());
        });

    Target Validate => _ => _
        .Executes(() =>
        {
            var valid = RootDirectory
                .GlobDirectories("{tips,scheduled}/**/*.{md,yml}")
                .Select(x => x.Parent)
                .Select(IsValid)
                .Aggregate(true, (a, t) => a && t);
            Assert.True(valid);
        });

    void ConfirmEdit(AbsolutePath file)
    {
        Assert.FileExists(file);

        ProcessTasks.StartShell($"open {file}").AssertZeroExitCode();

        do
        {
            Log.Information("Edit {File} and press key to commit", RootDirectory.GetRelativePathTo(file));
            Console.ReadKey();
        } while (!IsValid(file.Parent));
    }

    bool IsValid(AbsolutePath directory)
    {
        Assert.DirectoryExists(directory);

        var hasErrors = false;

        var markdownFiles = directory.GlobFiles("*.md");
        foreach (var markdownFile in markdownFiles)
        {
            var length = markdownFile.ReadAllText().Length;
            if (length > MaximumTweetLength)
            {
                Log.Error("{File} exceeds tweet limit ({Count})", markdownFile, length);
                hasErrors = true;
            }
        }

        var mediaFiles = directory.GlobFiles("*.{png,jpg,jpeg,gif}");
        foreach (var mediaFile in mediaFiles)
        {
            var size = mediaFile.ToFileInfo().Length;
            if (size > MaximumMediaSize)
            {
                Log.Error("{File} exceeds 2MB Slack preview limit ({Size})", mediaFile, size);
                hasErrors = true;
            }
        }

        return hasErrors;
    }
}
