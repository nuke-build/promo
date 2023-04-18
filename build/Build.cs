using System;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.Tools.AzureKeyVault;
using Nuke.Utilities.Text.Yaml;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>();

    public Build()
    {
        YamlExtensions.DefaultDeserializerBuilder = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance);
    }

    [GitRepository] readonly GitRepository Repository;

    [AzureKeyVaultConfiguration(
        BaseUrlParameterName = nameof(AzureKeyVaultBaseUrl),
        TenantIdParameterName = nameof(AzureKeyVaultTenantId),
        ClientIdParameterName = nameof(AzureKeyVaultClientId),
        ClientSecretParameterName = nameof(AzureKeyVaultClientSecret))]
    readonly AzureKeyVaultConfiguration AzureKeyVault;

    [Parameter] readonly string AzureKeyVaultBaseUrl;
    [Parameter] readonly string AzureKeyVaultTenantId;
    [Parameter] readonly string AzureKeyVaultClientId;
    [Parameter] [Secret] readonly string AzureKeyVaultClientSecret;

    class GitHubActionsWithKeyVaultAttribute : GitHubActionsAttribute
    {
        public GitHubActionsWithKeyVaultAttribute(string name)
            : base(name, GitHubActionsImage.UbuntuLatest)
        {
            EnableGitHubToken = true;
            ImportSecrets = new[]
            {
                nameof(AzureKeyVaultBaseUrl),
                nameof(AzureKeyVaultTenantId),
                nameof(AzureKeyVaultClientId),
                nameof(AzureKeyVaultClientSecret),
                nameof(WorkflowAccessToken)
            };
            CacheKeyFiles = Array.Empty<string>();
        }
    }
}
