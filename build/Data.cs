using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Nuke.Common.Tooling;

public record Post
{
    public string Title;

    [MaxLength(280)]
    public string Text;

    [RegularExpression("^https?:\\/\\/(?:www\\.)?[-a-zA-Z0-9@:%._\\+~#=]{1,256}\\.[a-zA-Z0-9()]{1,6}\\b(?:[-a-zA-Z0-9()@:%_\\+.~#?&\\/=]*)$")]
    public string ReadMore;
}

[JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public record Post2
{
    [Required]
    public string Title;

    [RegularExpression("\\d+.\\d+")]
    public string Version;

    public Products[] Products;
    public OperatingSystem OperatingSystem;
    public Technology[] Technologies;
    public Topic[] Topics;

    public bool Fun;
    public string[] Hashtags;
    public string ReadMore;
    public string Text;
    public string Tweet;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Products
{
    ReSharper,
    Rider,
    dotPeek,
    dotMemory,
    dotTrace,
    RiderFlow
}

[JsonConverter(typeof(StringEnumConverter))]
public enum OperatingSystem
{
    Windows,
    Linux,
    macOS
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Technology
{
    [EnumValue("ASP.NET")]
    AspNet,
    [EnumValue("AWS")]
    Aws,
    Azure,
    Blazor,
    [EnumValue("C#")]
    CSharp,
    [EnumValue("F#")]
    FSharp,
    DotNet,
    [EnumValue("MAUI")]
    Maui,
    Unity,
    Unreal
}

[JsonConverter(typeof(StringEnumConverter))]
public enum Topic
{
    Appearance,
    Completion,
    Data,
    Debugging,
    Editing,
    Inspections,
    Navigation,
    Profiling,
    Refactoring,
    Running,
    Testing,
    Vcs,
    Web
}
