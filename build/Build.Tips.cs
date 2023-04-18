using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.Utilities.Text.Yaml;
using Serilog;
using Spectre.Console;
using RequiredAttribute = Nuke.Common.RequiredAttribute;

[GitHubActionsWithKeyVault(
    "post-tip-dispatch-update",
    OnPushIncludePaths = new[] { "tips/*/*" },
    InvokedTargets = new[] { nameof(UpdateDispatchTip) })]
[GitHubActionsPostTipDispatch(
    PostTipDispatchWorkflow,
    AutoGenerate = false,
    InvokedTargets = new[] { nameof(ChooseTip) },
    OnWorkflowDispatchRequiredInputs = new[] { nameof(TipName) })]
partial class Build
{
    const string PostTipDispatchWorkflow = "post-tip-dispatch";
    Tool ThisBuild => ToolResolver.GetPathTool(BuildAssemblyFile);
    [Parameter] [Secret] readonly string WorkflowAccessToken;

    Target UpdateDispatchTip => _ => _
        .Requires(() => WorkflowAccessToken)
        .Executes(() =>
        {
            ThisBuild.Invoke($"--generate-configuration GitHubActions_{PostTipDispatchWorkflow} --host GitHubActions");

            CommitAndPush($"Update {nameof(ChooseTip)} workflow", RootDirectory / ".github", token: WorkflowAccessToken);
        });

    [Parameter] readonly string TipName;
    AbsolutePath TipsDirectory => RootDirectory / "tips";
    AbsolutePath TipsNewDirectory => TipsDirectory / "_new";

    Target ChooseTip => _ => _
        .Triggers(Post)
        .Executes(() =>
        {
            Log.Information(TipName);
        });

    Target CreateTip => _ => _
        .Executes(() =>
        {
            var post = new Post2();
            var fields = post.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var memberType = field.GetMemberType();
                if (memberType is { IsArray: true } && memberType.GetElementType() is { IsEnum: true } elementType)
                {
                    var enumValues = elementType.GetEnumValues<object>();
                    var values = AnsiConsole.Prompt(
                        new MultiSelectionPrompt<object>()
                            .Title($"{field.Name}?")
                            .HighlightStyle(new Style(Color.Turquoise2))
                            // TODO: add converter
                            .AddChoices(enumValues));
                    var genericValues = Array.CreateInstance(elementType, values.Count);
                    Array.Copy(values.ToArray(), genericValues, values.Count);
                    field.SetValue(post, genericValues);
                    AnsiConsole.WriteLine($"{field.Name}? {values.Select(x => x.ToString()).JoinSpace()}");
                }
                else if (memberType is { IsArray: true } && memberType.GetElementType() == typeof(string))
                {
                    var values = AnsiConsole.Prompt(
                        new TextPrompt<string>($"{field.Name}? (space-separated)")
                            .AllowEmpty());
                    field.SetValue(post, values.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray());
                }
                else if (memberType == typeof(bool))
                {
                    var value = AnsiConsole.Confirm($"{field.Name}?", defaultValue: false);
                    if (value)
                        field.SetValue(post, value);
                }
                else
                {
                    var value = AnsiConsole.Prompt(
                        new TextPrompt<string>($"{field.Name}?")
                            .When(!field.HasCustomAttribute<RequiredAttribute>(), _ => _
                                .AllowEmpty())
                            .Validate(x =>
                            {
                                if (field.GetCustomAttribute<RegularExpressionAttribute>() is { } attribute)
                                {
                                    return Regex.IsMatch(x, attribute.Pattern);
                                }

                                return true;
                            }));
                    if (!value.IsNullOrWhiteSpace())
                        field.SetValue(post, value);
                }
            }

            TipsNewDirectory.CreateDirectory();
            var tipFile = TipsNewDirectory / post.Title.Replace(" ", "-").ToLowerInvariant() / "index.yml";
            tipFile.WriteYaml(post);

            ConfirmEdit(tipFile);
            CommitAndPush($"Add {tipFile.Parent.NotNull().Name}", TipsDirectory);
        });

    class GitHubActionsPostTipDispatchAttribute : GitHubActionsAttribute
    {
        public GitHubActionsPostTipDispatchAttribute(string name)
            : base(name, GitHubActionsImage.UbuntuLatest)
        {
        }

        protected override IEnumerable<GitHubActionsDetailedTrigger> GetTriggers()
        {
            return new[] { new GitHubActionsWorkflowDispatchTrigger() };
        }

        class GitHubActionsWorkflowDispatchTrigger : Nuke.Common.CI.GitHubActions.Configuration.
            GitHubActionsWorkflowDispatchTrigger
        {
            public GitHubActionsWorkflowDispatchTrigger()
            {
                RequiredInputs = new string[0];
                OptionalInputs = new string[0];
            }

            public override void Write(CustomFileWriter writer)
            {
                base.Write(writer);

                using (writer.Indent())
                using (writer.Indent())
                {
                    writer.WriteLine($"{nameof(TipName)}:");
                    using (writer.Indent())
                    {
                        writer.WriteLine("type: choice");
                        writer.WriteLine("description: Tip");
                        writer.WriteLine("options:");
                        using (writer.Indent())
                        {
                            RootDirectory.GlobFiles("scheduled/*")
                                .Select(x => x.NameWithoutExtension)
                                .OrderBy(x => x)
                                .ForEach(x => writer.WriteLine($"- {x.SingleQuote()}"));
                        }
                    }
                }
            }
        }
    }
}
