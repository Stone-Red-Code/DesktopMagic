using Modio.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DesktopMagic.Plugins;

public class PluginMetadata
{
    public string Name { get; set; } = string.Empty;

    public uint Id { get; set; }

    public string? Author { get; set; }

    public Uri? IconUri { get; set; }

    public Uri? ProfileUri { get; set; }

    public DateTime? Added { get; set; }

    public DateTime? Updated { get; set; }

    public string? Description { get; set; }

    public string? Summary { get; set; }

    public string? Version { get; set; }

    public List<string> Tags { get; set; } = [];

    [JsonIgnore]
    public bool SupportsUnloading => !Tags.Contains("Does Not Support Unloading");

    [JsonIgnore]
    public bool IsLocalPlugin => string.IsNullOrWhiteSpace(ProfileUri?.ToString());

    public PluginMetadata(Mod mod)
    {
        Name = mod.Name ?? mod.Id.ToString();
        Id = mod.Id;
        Author = mod.SubmittedBy?.Username;
        IconUri = mod.Logo?.Thumb320x180;
        ProfileUri = mod.ProfileUrl;
        Added = DateTimeOffset.FromUnixTimeSeconds(mod.DateAdded).DateTime;
        Updated = DateTimeOffset.FromUnixTimeSeconds(mod.DateUpdated).DateTime;
        Description = mod.DescriptionPlaintext;
        Summary = mod.Summary;
        Version = mod.Modfile?.Version;
        Tags = mod.Tags.Select(t => t.Name).Where(t => t is not null).ToList()!;
    }

    public PluginMetadata(string name, uint id)
    {
        Name = name;
        Id = id;
    }

    [JsonConstructor]
    public PluginMetadata()
    {
    }

    public override bool Equals(object? obj)
    {
        if (obj is not PluginMetadata other)
        {
            return false;
        }

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}