using System.Linq;
using ImageMagick;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.AzureKeyVault;
using Nuke.Common.Tools.Mastodon;

partial class Build
{
    readonly string MastodonInstanceUrl = "https://dotnet.social";
    [AzureKeyVaultSecret] readonly string MastodonAccessToken = "PD3zziDvj9WOCaAetdLEcJ7eAlcqmPt63wiXXGGhGv4";

    Target PostMastodon => _ => _
        .DependsOn(ChooseScheduled)
        .Requires(() => MastodonAccessToken)
        .ProceedAfterFailure()
        .Executes(async () =>
        {
            var text = MarkdownFile.ReadAllText().ToPlatform("mastodon").ToPlainText();

            // https://github.com/dlemstra/Magick.NET/issues/287
            var mediaFiles = MediaFiles
                .Select(originalFile =>
                {
                    MagickGeometry GetResolution(IMagickImage image)
                        => image.Width > image.Height
                            ? new MagickGeometry(1280, 0)
                            : new MagickGeometry(0, 1280);

                    var resizedFile = TemporaryDirectory / originalFile.Name;

                    if (originalFile.HasExtension("png", "jpg", "jpeg"))
                    {
                        using var image = new MagickImage(originalFile.ToFileInfo());
                        if (image.Width <= 1280 && image.Height <= 1280)
                            return originalFile;

                        image.Resize(GetResolution(image));
                        image.Write(resizedFile);
                        return resizedFile;
                    }

                    if (originalFile.HasExtension("gif"))
                    {
                        using var collection = new MagickImageCollection(originalFile.ToFileInfo());
                        collection.Coalesce();
                        foreach (var image in collection)
                            image.Resize(GetResolution(image));

                        collection.Optimize();
                        collection.Write(resizedFile);
                        return resizedFile;
                    }

                    return originalFile;
                }).ToList();

            await MastodonTasks.SendMastodonMessageAsync(_ => _
                    .SetText(text)
                    .AddMediaFiles(mediaFiles.Select(x => x.ToString())),
                MastodonInstanceUrl,
                MastodonAccessToken);
        });
}
