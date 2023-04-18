using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.AzureKeyVault;

partial class Build
{
    [AzureKeyVaultSecret] readonly string SlackTestWebhook;

    Target PostSlack => _ => _
        .Requires(() => SlackTestWebhook)
        .ProceedAfterFailure()
        .Executes(async () =>
        {
            var attachment = new Attachment
            {
                Text = MarkdownFile.ReadAllText().ToPlatform("slack")
            };

            var result = await SlackTestWebhook
                .PostJsonAsync(new { attachments = new[] { attachment } })
                .ReceiveString();

            Assert.True(result == "ok");
        });

    [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
    public record Attachment
    {
        public string Fallback;
        public string Text;
        public string AuthorName;
        public string AuthorIcon;
        public string AuthorLink;
        public string AuthorSubname;
        public string ImageUrl;
        public string Footer;
    }
}
