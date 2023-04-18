using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Nuke.Common.Utilities;

public static class Extensions
{
    public static string ToPlatform(this string text, string platform)
    {
        return text.ReplaceRegex(@"\!\[\[(?<replacements>.*)\]\]", x =>
        {
            var replacement = Enumerable.Select<string, string[]>(x.Groups["replacements"].Value.Split(new[] { '|', ';' }, StringSplitOptions.TrimEntries), x => x.Split(':'))
                .Select(x => x.Length == 2 ? (x[0], x[1]) : (string.Empty, x[0]))
                .ToDictionary(x => x.Item1, x => x.Item2);

            if (replacement.TryGetValue("twitter", out var twitterHandle))
                replacement["twitter"] = $"@{twitterHandle.TrimStart("@")}";
            if (replacement.TryGetValue("mastodon", out var mastodonHandle))
                replacement["mastodon"] = $"@{mastodonHandle.TrimStart("@")}";
            if (replacement.TryGetValue("slack", out var slackUserId))
                replacement["slack"] = $"<@{slackUserId.TrimStart("@")}>";

            return replacement.GetValueOrDefault(platform) ?? replacement[string.Empty];
        });
    }

    public static string ToPlainText(this string text)
    {
        return Markdown.ToPlainText(text);
    }

    public static string ToIsoDate(this DateTime date)
    {
        return date.ToString("yyyy-MM-dd");
    }
}
