using System.Linq;
using System.Threading.Tasks;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.AzureKeyVault;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Parameters;

partial class Build
{
    [AzureKeyVaultSecret] readonly string TwitterConsumerKey;
    [AzureKeyVaultSecret] readonly string TwitterConsumerSecret;
    [AzureKeyVaultSecret] readonly string TwitterAccessToken;
    [AzureKeyVaultSecret] readonly string TwitterAccessTokenSecret;

    // https://twittercommunity.com/t/how-do-i-change-my-app-from-read-only-to-read-write/163624/3
    // https://developer.twitter.com/en/docs/twitter-api/v1/media/upload-media/uploading-media/media-best-practices
    // https://superface.ai/blog/twitter-api-new-plans#can-i-post-tweets-with-media-images-gifs-videos
    Target PostTwitter => _ => _
        .Requires(() => TwitterAccessToken)
        .Requires(() => TwitterAccessTokenSecret)
        .Requires(() => TwitterConsumerKey)
        .Requires(() => TwitterConsumerSecret)
        .ProceedAfterFailure()
        .Executes(async () =>
        {
            var text = MarkdownFile.ReadAllText().ToPlatform("twitter").ToPlainText();

            var client = new TwitterClient(
                new TwitterCredentials(
                    TwitterConsumerKey,
                    TwitterConsumerSecret,
                    TwitterAccessToken,
                    TwitterAccessTokenSecret));

            var medias = MediaFiles.Select(
                async x => await client.Upload.UploadTweetImageAsync(
                    new UploadTweetImageParameters(x.ReadAllBytes())
                    {
                        MediaCategory = MediaCategory.Image
                    })).ToArray();
            Task.WaitAll(medias);

            await client.Tweets.PublishTweetAsync(
                new PublishTweetParameters
                {
                    Text = text,
                    Medias = medias.Select(x => x.Result).ToList()
                });
        });
}
