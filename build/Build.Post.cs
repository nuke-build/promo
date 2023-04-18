using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;

partial class Build
{
    AbsolutePath MarkdownFile;
    AbsolutePath YamlFile;
    IEnumerable<AbsolutePath> MediaFiles;

    Target Post => _ => _
        .OnlyWhenDynamic(() => MarkdownFile != null || YamlFile != null)
        .Triggers(PostSlack)
        .Triggers(PostMastodon)
        .Triggers(PostTwitter)
    ;
}
